# Deployment-agent demo

One-command demo of Pulumi's **prebuilt** customer-managed workflow agent
(`pulumi/customer-managed-workflow-agent`) running deployments against HappyPumi — no HappyPumi-side
runner code, just the API endpoints the agent calls (reverse-engineered black-box; see ADR-0008).

It composes four containers:

| Service | What it is |
|---|---|
| `postgres` | HappyPumi's state store (ADR-0005). |
| `happypumi` | The API, built from `HappyPumi.Api/Dockerfile`, seeded with the `happypumi/webstore/prod` stack. Published on host `:5118`. |
| `agent` | The unmodified prebuilt agent. Polls HappyPumi, claims deployments, and runs each in an executor container it launches via the host Docker socket. |
| `gitserver` | A `git daemon` serving the resourceless `empty-stack` program at `git://172.17.0.1:9418/empty-stack.git`, so remote-workspace deployments have a repo to clone with no public-internet dependency. |

## Remote-workspace (git source) deployments

A deployment created with a **git source** (`pulumi up --remote` or the Automation API's
`NewRemoteStackGitSource`) sends a `sourceContext.git` body. HappyPumi persists the repo URL / branch /
dir on the deployment, and the runner job builder emits a `git clone … && pulumi <op> --yes` step that
targets HappyPumi as its backend. Trigger one against the bundled git server:

```bash
curl -s -X POST http://localhost:5118/api/stacks/happypumi/webstore/prod/deployments \
     -H 'Authorization: token t' -H 'Content-Type: application/json' \
     -d '{"operation":"update","sourceContext":{"git":{"repoUrl":"git://172.17.0.1:9418/empty-stack.git","branch":"master"}}}'
```

This path is covered end-to-end by the Docker-backed `RemoteWorkspaceTests` in
`HappyPumi.AutomationApi.IntegrationTests` (auto-skips when Docker is unavailable).

## Run it

```bash
docker compose -f deploy/deployment-agent/docker-compose.yml up --build -d

# trigger a deployment
curl -s -X POST http://localhost:5118/api/stacks/happypumi/webstore/prod/deployments \
     -H 'Authorization: token t' -H 'Content-Type: application/json' -d '{"operation":"update"}'

# watch the agent claim + run it
docker compose -f deploy/deployment-agent/docker-compose.yml logs -f agent
```

You'll see the agent log `Running deployment workflow '<id>'`, create + start the `pulumi/pulumi-base`
executor container, and `Job completed`. The deployment row ends `succeeded`:

```bash
curl -s -X POST "http://localhost:5118/api/agent-workflows/deployment:<id>/check" -H 'Authorization: token t'
# {"id":"<id>","status":"succeeded","complete":true}
```

Tear down:

```bash
docker compose -f deploy/deployment-agent/docker-compose.yml down -v
```

## Topology (why it's wired this way — Linux)

The agent launches executor (and nested per-step) containers via the **host** Docker daemon, so the wiring
has to make one `service_url` and one workdir path valid from the host, the agent, and those spawned
containers at once:

- **`network_mode: host`** on the agent + **`service_url: http://172.17.0.1:5118`** (the docker0 gateway):
  reachable from the host-network agent *and* from the bridged executor/step containers it spawns.
  `localhost` would point at each container itself; a compose service name wouldn't resolve from the
  spawned containers (they're not on the compose network).
- **`/tmp/hp-shared` bind-mounted at the same path** in the agent (`shared_volume_directory`): job workdirs
  the agent creates there exist on the host, so the executor's bind-mount of each workdir resolves.
- **`/var/run/docker.sock`** mounted: `deploy_target: docker` launches executor containers.
- The shipped image has `workflow-runner` but not `workflow-runner-embeddable`; the agent entrypoint stages
  both into `/runners` (`working_directory`) before `customer-managed-workflow-agent run`.

The deployment's step runs in the executor; a real source-backed `pulumi up` (clone repo + `pulumi <op>
--yes`) targets HappyPumi as its backend and so exercises only the already-implemented Tier-1 update
lifecycle endpoints. The default step here is `pulumi version` (smoke); see `GetWorkflowJobEndpoint`.

> macOS/Windows note: `172.17.0.1` is Linux-specific. On Docker Desktop, the nested-container networking
> differs; use `host.docker.internal` and adjust as needed.
