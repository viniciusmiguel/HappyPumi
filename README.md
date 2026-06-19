# HappyPumi

**An open-source, wire-compatible reimplementation of the [Pulumi Cloud](https://www.pulumi.com/product/pulumi-cloud/) API.**

HappyPumi is a self-hostable backend for the Pulumi ecosystem. A stock `pulumi`
CLI can `login`, manage stacks and config, and run the full update lifecycle
(`up` / `preview` / `refresh` / `destroy`) against it — plus the higher-level
Internal Developer Platform features: organizations and RBAC, an ESC secrets/config
engine, a private registry, CrossGuard policy, managed deployments, and a web
console that mirrors the real Pulumi Cloud UI.

> **License:** Apache-2.0 &nbsp;·&nbsp; **Runtime:** .NET 10 / C# &nbsp;·&nbsp; **Database:** PostgreSQL

---

## Why?

Pulumi's CLI and SDKs are open source, but the cloud backend that gives you
state management, RBAC, policy, ESC, and the web console is a proprietary,
hosted service. HappyPumi is an independent, **clean-room** implementation of
that backend's HTTP contract so you can run the whole platform yourself.

> **Clean-room:** HappyPumi is derived *only* from two public sources — the
> public Pulumi Cloud OpenAPI spec (`pulumi-spec.json`) and the Apache-2.0-licensed
> Pulumi CLI, treated as a black-box client. No proprietary server source is used,
> referenced, copied, or decompiled. See [`docs/adr/0008`](docs/adr/0008-clean-room-implementation.md).

---

## What works today

| Area | Status |
|---|---|
| **Auth & RBAC** | OIDC login (Dex) for the console + the CLI `token` scheme; per-org members, custom roles, and **teams**; 157 resource\:action permissions enforced per endpoint. |
| **Stacks & state** | Full update lifecycle, checkpoints, state export/import, config, update history with the real requesting user, and live stack metadata. |
| **ESC environments** | Pulumi ESC environments with a real evaluator (interpolation, `fn::secret`, imports) + a resolved-value preview. |
| **Private registry** | `pulumi package publish` and `pulumi template publish` end-to-end (schema, README, nav, archives) with real blob storage; live component usage counts. |
| **Managed deployments** | A UI/API-triggered deployment runs on the prebuilt Pulumi **workflow runner**: it fetches a published template and runs `pulumi up`, streaming a real step timeline and logs back to the console. |
| **CrossGuard policy** | `pulumi policy publish` + `pulumi policy enable`, with the runner **enforcing** the pack during a deployment; violations surface on a **Policy findings** page. |
| **Web console** | A React + Tailwind clone of the Pulumi Cloud console (`console/`): dashboard, stacks, environments, deployments, registry, policy, and access-management pages — all on real API data. |
| **Audit & platform** | Audit log of infrastructure-changing actions (ADR-0010); services, cloud accounts, VCS connections, identity providers, and approval rules. |

The authoritative API surface is **[`pulumi-spec.json`](pulumi-spec.json)** (the
Pulumi Cloud OpenAPI spec). [`ENDPOINTS.md`](ENDPOINTS.md) is the
reverse-engineering roadmap — which endpoints to implement, in priority order.

---

## Quickstart

Prerequisites: **.NET 10 SDK**, **Docker** (for Postgres + Dex via .NET Aspire),
and **Node 20+** (for the console).

```bash
# 1. Trust the self-signed HTTPS dev cert, then bring up the whole topology
#    (Postgres + Dex OIDC + the API) with the Aspire dashboard:
make dev

# 2. In another terminal, run the web console:
cd console && npm install && npm run dev      # → http://localhost:5173

# 3. Point a real pulumi CLI at your local backend:
pulumi login http://localhost:5118
```

Sign in to the console with the seeded Dex user `admin@happypumi.dev` /
`password`. An end-to-end IdP demo (a C# component, a template, and a policy
pack) lives in [`examples/idp-demo/`](examples/idp-demo).

---

## Run the published container image

Every release publishes the API image to GHCR. Bring up the API and its database
with one command using [`compose.yaml`](compose.yaml) — no source build required:

```bash
docker compose up -d            # API → http://localhost:5118, seeded with demo data
docker compose logs -f api      # watch startup (it applies the schema migration on boot)
docker compose down -v          # stop everything and wipe the database volume
```

The image is public: `ghcr.io/<owner>/happypumi-api:latest` (or a pinned tag like
`:0.1.0`). Demo data (orgs, stacks, registry, a policy pack) is seeded so there's
something to query immediately.

### Authentication

The API accepts two credential types (ADR-0007), dispatched by the `Authorization`
header:

- **`token <anything>`** — the Pulumi CLI scheme. In this dev image any non-empty
  token authenticates as the seeded **admin**, so no IdP is needed. Use
  `token role:member:alice` to act as a non-admin and exercise RBAC.
- **`Bearer <jwt>`** — an OIDC id-token from Dex, used by the console's browser
  sign-in. This is only wired when Dex is present (`compose.full.yaml` / `make dev`).

`compose.yaml` ships **only the token scheme**, which is all the CLI and REST need:

```bash
# REST — any token = seeded admin
curl -s -H "Authorization: token hp-dev" http://localhost:5118/api/user
curl -s -H "Authorization: token hp-dev" http://localhost:5118/api/user/stacks

# pulumi CLI
export PULUMI_ACCESS_TOKEN=hp-dev
pulumi login http://localhost:5118
pulumi whoami        # → happypumi
```

### Full topology with console login

To use the console's interactive Dex sign-in from containers, bring up the whole
stack (Postgres + Dex + the API image + the console) with
[`compose.full.yaml`](compose.full.yaml):

```bash
docker compose -f compose.full.yaml up      # console → http://localhost:5173
# sign in with admin@happypumi.dev / password
```

**Linux only.** Every service uses host networking so that `http://localhost:5556`
(the issuer + JWKS baked into Dex's tokens) resolves to the same Dex for both the
browser and the API container — the one way to satisfy OIDC reachability without
code changes. On **macOS / Windows** (Docker Desktop), use `make dev` for the full
console experience; `compose.yaml` remains the cross-platform way to exercise the
API via REST or the pulumi CLI.

---

## Architecture

| Project | Purpose |
|---|---|
| `HappyPumi.Api` | The API. One [FastEndpoints](https://fast-endpoints.com/) endpoint per spec operation, organized into area folders. DTOs in `Contracts/`, persistence seams in `State/`, EF Core in `Data/`. |
| `HappyPumi.AppHost` | .NET Aspire composition root: Postgres (+ pgWeb), Dex (OIDC), and the API over HTTPS. This is `make dev`. |
| `HappyPumi.ServiceDefaults` | Shared Aspire defaults: OpenTelemetry, health checks, service discovery, resilience. |
| `HappyPumi.Api.Tests` | xUnit component tests — the real API in-process against a throwaway Postgres (Testcontainers). |
| `HappyPumi.Cli.IntegrationTests` | Drives the **real** `pulumi` CLI against a live HappyPumi over HTTPS (wire-compatibility). |
| `Generator` | Reads the OpenAPI spec and emits endpoint + contract scaffolding. |
| `console/` | The React + Vite + Tailwind web console. |

Key decisions live in [`docs/adr/`](docs/adr) (MADR format, one decision per
file): .NET 10, FastEndpoints, PostgreSQL + `jsonb`, OpenTelemetry, OIDC + RBAC,
clean-room sourcing, multi-VCS support, and fail-closed audit logging.

---

## Build, run, test

```bash
make build              # build the whole solution
make dev                # full Aspire topology + dashboard
make test-unit          # fast in-process component tests (needs Docker)
make test-integration   # drive the real pulumi CLI against a live HappyPumi
make coverage           # tests with coverage
make docker             # build the production container image
make help               # list every target
```

No cloud infrastructure is needed for tests: login/stack/config/export hit the
backend directly, and the update lifecycle is exercised by a **resourceless** Go
program, so it runs offline. The dev/test environment uses HTTPS with the
self-signed ASP.NET Core dev certificate (`make certs`).

---

## Versioning & releases

HappyPumi follows [Semantic Versioning](https://semver.org). The baseline version
lives in [`Directory.Build.props`](Directory.Build.props) and every assembly inherits it.

Releases are cut by tagging a commit on `main`:

```bash
git tag v0.1.0      # vMAJOR.MINOR.PATCH
git push origin v0.1.0
```

Pushing a `v*` tag triggers the CI release job, which runs the unit + integration
suites and then builds and publishes the API container image to GHCR
(`ghcr.io/<owner>/happypumi-api`) tagged with the SemVer (`0.1.0`) and `latest`.
Pushes to `main` publish a rolling `edge` image. CI is defined in
[`.github/workflows/ci.yml`](.github/workflows/ci.yml).

---

## Contributing

1. Find the priority/tier of an endpoint in [`ENDPOINTS.md`](ENDPOINTS.md);
   implement Tier 0 → 1 first (login + the update lifecycle).
2. Replace the generated `AllowAnonymous()` stub with a real auth policy
   (ADR-0007) and land a behavioral test — an endpoint isn't "done" while it's
   still anonymous or untested.
3. Honor the clean-room rule (ADR-0008): derive behavior only from the public
   spec and the open-source CLI, never from Pulumi's proprietary internals.

Generated files are marked `// <auto-generated />`; the generator overwrites
them unconditionally, so hand-written handler bodies carry a header noting they
were implemented by hand.

---

## License

[Apache-2.0](LICENSE). Pulumi, Pulumi Cloud, and CrossGuard are trademarks of
Pulumi Corporation; HappyPumi is an independent project and is not affiliated
with or endorsed by Pulumi.
