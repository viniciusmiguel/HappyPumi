# Automation API support — design

> Status: approved (brainstorm) · 2026-06-29 · branch `feat/automation-api-support`

## Goal

Make HappyPumi a proven backend for the **Pulumi Automation API** — the programmatic
interface for driving Pulumi programs without the CLI
(<https://www.pulumi.com/docs/iac/guides/building-extending/automation-api/>). The chosen
scope is a **full feature sweep**: systematically exercise every Automation-API operation
against HappyPumi, using the **Go auto SDK** (`github.com/pulumi/pulumi/sdk/v3/go/auto`),
and harden every gap it surfaces — including the **remote-workspace** path.

## Key finding (why this is mostly verification + targeted gaps)

The Automation API is not a separate wire protocol. The Go SDK README states it
"encapsulates the functionality of the CLI … This still requires a CLI binary to be
installed and available on your $PATH." All four SDKs (Go/Node/Python/.NET) shell out to
the local `pulumi` binary. Therefore, against a Service backend (HappyPumi), the auto API
makes the **same HTTP calls the CLI already makes**. HappyPumi already implements the full
CLI lifecycle (Tiers 0–7 merged), so most auto operations already work; "support" means
*proving* coverage and *filling specific gaps*.

## Approach (approved: Option A)

A **fourth integration-test layer** — `HappyPumi.AutomationApi.IntegrationTests/` —
structurally identical to the existing CLI wire-compat layer
(`HappyPumi.Cli.IntegrationTests`), but the client is the Go auto SDK instead of raw
`pulumi` invocations.

- A Go module (`autoapi/*_test.go`) using `sdk/v3/go/auto`, resolved against the local
  `../pulumi` SDK so it builds offline (same as `make pulumi`).
- A thin xUnit wrapper that boots the existing `HappyPumiServer` (HTTPS), provisions a
  token + org, sets `PULUMI_BACKEND_URL` / `PULUMI_ACCESS_TOKEN` / `PATH=.tools/bin` /
  `SSL_CERT_DIR`, runs `go test -json`, and surfaces each subtest as a readable xUnit line.
- Reuses `HappyPumiServer`, `DevCertTrust`, and token/org provisioning from
  `tests-support/` (shared by link, not copied).

Rejected: (B) standalone Go suite + shell script — diverges from the xUnit harness, can't
share the proven server bootstrap/fixtures; (C) .NET Automation SDK — user chose Go, and it
still shells out to the CLI.

## Operation coverage map

The auto-SDK surface (`Workspace` interface + `Stack` methods) splits in two.

**Local-only (no HappyPumi traffic; validated implicitly by the lifecycle running green,
not directly asserted):** `ProjectSettings`/`SaveProjectSettings`,
`StackSettings`/`SaveStackSettings`, env-var ops, `WorkDir`/`PulumiHome`/`PulumiVersion`,
`Program`/`SetProgram`, `Install`/`New`, plugin ops, and the `config` get/set/remove family
(these edit local `Pulumi.<stack>.yaml`; the service config endpoints exist but are not on
this path).

**Backend-touching (each gets a dedicated auto-SDK subtest):**

| Auto-SDK op | Endpoint(s) | Status |
|---|---|---|
| `WhoAmI` / `WhoAmIDetails` | `GetCurrentUser` | implemented |
| `OrgGetDefault` | `GetDefaultOrganization` | implemented |
| `CreateStack` / `SelectStack` / `Stack`/`Info` | `CreateStack` / `GetStack` | implemented |
| `ListStacks` | `ListUserStacks` | implemented |
| `RemoveStack` | `DeleteStack` | implemented |
| `Rename` | `RenameStack` | implemented |
| `Up` / `Preview` / `Refresh` / `Destroy` / `PreviewRefresh` / `PreviewDestroy` | full update lifecycle (4 kinds) | implemented |
| `ImportResources` | update lifecycle + import | verify |
| `Cancel` | `CancelUpdate_*` | verify (stack-level path) |
| `History` | `GetStackUpdates` | implemented |
| `Outputs` / `StackOutputs` | `ExportStack` | implemented |
| `Export` / `Import` | `ExportStack` / `ImportStack` | implemented |
| `SetTag` | `AddStackTag` | implemented |
| `ListTags` / `GetTag` | `GetStack` (tags) | implemented |
| **`RemoveTag`** | **`DeleteStackTag`** | **stub — gap** |
| `ChangeSecretsProvider` | export → re-encrypt → import + config | verify |
| `AddEnvironments`/`ListEnvironments`/`RemoveEnvironment` | ESC refs (`ListEnvironments_esc`) | verify |

Confirmed hard gap: `DeleteStackTag` (and likely `UpdateStackTag`). The `ImportResources`,
`Cancel`, `ChangeSecretsProvider`, and ESC-env-ref rows are verify-first — implement only if
a subtest goes red.

## Phase A — local/inline sweep + endpoint gaps

For each gap the sweep surfaces, follow the repo's TDD loop (CLAUDE.md):

1. **Component test first** (`HappyPumi.Api.Tests`, in-process `WebApplicationFactory`):
   assert the endpoint's wire contract (route, status, request/response shape) against the
   OpenAPI spec. Red.
2. **Implement** the `HandleAsync` body against Postgres, mirroring the sibling endpoint
   (e.g. `DeleteStackTag` mirrors `AddStackTag`/`UpdateStackTags`). Green.
3. **Auto-SDK subtest** in the Go layer drives the operation end-to-end (e.g.
   `SetTag → ListTags → RemoveTag → ListTags` asserts removal).

Known work item up front: **`DeleteStackTag`** (+ `UpdateStackTag` if `SetTag` routes to the
single-tag PATCH). Generated `// <auto-generated />` files are never hand-edited without
accounting for the generator-overwrite caveat (HappyPumi/CLAUDE.md).

Fixture: the existing resourceless **`empty-stack`** Go program drives
`Up`/`Preview`/`Refresh`/`Destroy` with zero real provisioning, reused for both
local-source and **inline-source** auto programs.

## Phase B — remote workspaces

Remote auto (`NewRemoteStackGitSource` → `RemoteStack.Up`) runs `pulumi up --remote
--remote-git-url …`, which POSTs a **git source** to `CreateAPIDeploymentHandlerV2` and
polls `GetDeployment`; the prebuilt agent claims and executes the job. The runner plumbing
(`WorkflowAgentEndpoints`, `IDeploymentQueue`, `deploy/deployment-agent/docker-compose.yml`)
is merged and the demo works — but today `CreateApiDeploymentHandlerV2` reads only
`operation` + `templateRef`, and `BuildJob` has two modes (template-archive, or a
`pulumi version` smoke step). **There is no git-source mode** — that is the gap.

**Backend work (TDD, component-tested):**

1. `CreateApiDeploymentHandlerV2` — parse `sourceContext.git` (repo URL, branch, repoDir)
   and `operationContext` from the body; persist them on the deployment.
2. `DeploymentRow` / `IDeploymentStore` / `IDeploymentQueue` — carry the git-source fields
   (EF migration).
3. `BuildJob` — add a **git-source mode**: `git clone <url> [-b branch]` → `cd repoDir` →
   `pulumi stack select --create` → `pulumi <op> --yes`, reusing the existing OWASP-A03
   identifier allow-list / single-quoting hardening already applied to `templateRef`.

**Remote integration test (Docker-backed; auto-skips when Docker is absent, matching
`AspireTopologyTests`):**

- Reuses the `deploy/deployment-agent/docker-compose.yml` topology (postgres + happypumi +
  the real prebuilt `pulumi/customer-managed-workflow-agent`).
- A Go driver uses `NewRemoteStackGitSource` + `RemoteStack.Up/Preview/Destroy` against
  HappyPumi, asserting the deployment reaches `succeeded` and `RemoteStack.Outputs()` /
  `History()` read back.

**Open design risk — hermetic clone source.** The clone must not depend on the public
internet (clean-room + offline CI). The resourceless `empty-stack` fixture is served from a
**local git source** inside the compose — either a small git-daemon container or a
file/bare repo on the shared volume. *Resolving the exact clone mechanism is the first task
of Phase B.*

## Test, build, and CI wiring

- New `make test-automation` target; folded into `make test`.
- The Go auto SDK shells out to the locally-built `pulumi` binary in `.tools/bin`
  (`make pulumi`), so CI must have the Go toolchain (already required for `make pulumi`).
- Coverage > 80% and duplication < 3% gates apply to the C# changes (the endpoint gaps),
  checked locally before the PR (CLAUDE.md).

## Out of scope

- Local-only auto ops (config/plugins/settings/env-vars) — not directly asserted; covered
  implicitly when the lifecycle runs green.
- Non-Go Automation SDKs (Node/Python/.NET) — the wire behavior is identical (all shell out
  to the CLI), so the Go layer proves the backend for all of them.

## Acceptance

- Every backend-touching auto operation in the coverage map has a green Go subtest against a
  live HappyPumi.
- `DeleteStackTag` (and any other gap the sweep finds) is implemented with a component test
  + regression coverage.
- A Docker-backed remote test drives a git-source `RemoteStack.Up` to `succeeded` against
  HappyPumi, and auto-skips cleanly without Docker.
- `make test` (incl. `make test-automation`) is green; coverage > 80%, duplication < 3%.
