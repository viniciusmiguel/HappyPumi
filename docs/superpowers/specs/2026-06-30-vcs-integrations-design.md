# VCS Integrations (GitHub + Azure DevOps) — design

> Status: approved (brainstorm) · 2026-06-30 · phased across 3 PRs

## Goal

Implement the console **VCS integrations** surface for the providers we actually target —
**github.com, GitHub Enterprise, and Azure DevOps** — on top of the `IVcsProvider` seam that
ADR-0009 decided but never built. GitLab, BitBucket, and custom VCS stay out of scope (their
endpoints remain stubs).

## Decisions (from brainstorm)

- **Real provider API calls.** `IVcsProvider` implementations make real GitHub/Azure DevOps
  REST + OAuth calls via an injected `HttpClient`; tests assert outgoing request shaping with
  the existing `Esc/StubHttpHandler` (no network), matching the ESC OIDC-exchanger pattern.
- **GitHub Enterprise included** as a GitHub variant — same `GitHubVcsProvider` with a
  configurable base URL.
- **Phased: 3 PRs**, each merged when CI is green.

## Scope (endpoints)

| PR | Group | Endpoints |
|---|---|---|
| 1 | Integration records (no external calls) | `ListAllVcsIntegrations`; `List`/`Get`/`Update`/`Delete` for `github`, `github-enterprise`, `azure-devops` |
| 2 | GitHub provider (real REST) | `StartGitHubSetup`, `GetGitHubAccess`, `ListGitHubOrganizationTeams`, `CreateGitHubTeam`, plus GitHub `ListVcsRepos`/`ListVcsBranches`/`ListVcsRepoDestinations` |
| 3 | Azure DevOps provider + OAuth | `CreateAzureDevOpsSetup`, `InitiateAzureDevOpsOAuth`, `CompleteAzureDevOpsOAuth`, `GetAzureDevOpsAccessStatus`, `ListAzureDevOpsOrganizations`, `ListAzureDevOpsProjects`, ADO repo/branch listing |

**Out of scope:** GitLab, BitBucket, custom-VCS endpoints (remain `NotImplementedException`).

## Backend architecture

New persistence seam + provider seam, following ADR-0005 (interface in `State/` with
`InMemory*` + `Postgres*` impls) and ADR-0009 (provider-neutral, no provider privileged).

- **`IVcsIntegrationStore`** — provider-neutral records:
  `{ Id, Org, Kind, Name, BaseUrl?, Credential (jsonb, write-only), CreatedBy, Created }`.
  `InMemoryVcsIntegrationStore` + `PostgresVcsIntegrationStore` (`VcsIntegrationRow` + EF
  migration). Backs all record CRUD (`ListAll`, per-kind list/get/update/delete). `Kind` is one
  of `github` | `github-enterprise` | `azure-devops`.
- **`IVcsProvider`** (owned interface): `GetAccessStatusAsync`, `ListReposAsync`,
  `ListBranchesAsync`, `ListRepoDestinationsAsync`, and OAuth `BuildAuthorizationUrl` +
  `ExchangeCodeAsync`. Implementations: `GitHubVcsProvider` (github.com + GitHub Enterprise via
  base URL), `AzureDevOpsVcsProvider`. Provider-specific reads (ADO orgs/projects, GitHub org
  teams) are methods on the concrete providers, called by their dedicated endpoints.
- **`IVcsProviderRegistry`** — resolves an `IVcsProvider` by integration `Kind`. The generic
  endpoints (`ListVcsRepos`/`ListVcsBranches`/`ListVcsRepoDestinations`) load the integration,
  pick the provider, and delegate.
- **External calls** go through `IHttpClientFactory`-provided `HttpClient`s. Tests inject a
  `StubHttpHandler` that asserts method/URL/headers/body and returns canned responses; prod uses
  real clients. No live network in tests.
- **OAuth (Azure DevOps):** `InitiateAzureDevOpsOAuth` returns the provider authorization URL
  (with a stored `state`); `CompleteAzureDevOpsOAuth` exchanges `code` for a token via the
  provider and persists it on the integration. GitHub uses the App install/access-status shape
  (`StartGitHubSetup` returns the install URL; `GetGitHubAccess` reports access-status).
- **Config:** `Vcs:GitHub:*` (app id / private key), `Vcs:AzureDevOps:*` (OAuth client id /
  secret), and per-kind base URLs. When unconfigured, access-status reports *not configured* and
  listings return empty (or 403) gracefully — the feature is safe to run without secrets.
- **Auth:** endpoints drop the generated `AllowAnonymous()` for the org RBAC policy (ADR-0007).

## Frontend

Wire the existing `console/src/pages/VersionControl.tsx` to the real endpoints (replacing the
placeholder `/orgs/{org}/vcs-connections` calls):

- List integrations across all in-scope providers; a **Connect** flow (GitHub App install link /
  Azure DevOps OAuth initiate → callback); per-integration detail with a repos/branches browser;
  and disconnect. Split into focused components under `console/src/pages/vcs/` if
  `VersionControl.tsx` would exceed 500 lines. New `api.*` fetchers grouped in `lib/api.ts`.

## Testing & process

- **Record CRUD** → component tests (`HappyPumi.Api.Tests`, `WebApplicationFactory`, real
  Postgres). New stores get in-memory unit tests.
- **Providers** → `StubHttpHandler` tests asserting the outgoing GitHub/ADO request shape with
  faked responses; OAuth exchange faked via the seam. No network.
- **Console** → `npm run build && npm run lint` per PR (not in CI).
- **Gates:** coverage > 80% on changed C#, duplication < 3%. Each PR branches off `main`, granular
  commits (Co-Authored-By trailer), merged when CI is green. No squash.
- Generated endpoint files: edit only the `HandleAsync` body (+ ctor injection); never the
  `// <auto-generated />` contract files — but note the generator's recursive-`Body` fix is now on
  `main`, so newly-touched request bodies are already correct.

## Acceptance

- Every in-scope endpoint returns real data / performs its action (no `NotImplementedException`),
  covered by a test; the console VCS page lists, connects, browses, and disconnects GitHub +
  Azure DevOps integrations and builds + lints clean.
- GitLab/BitBucket/custom endpoints remain stubs (explicitly out of scope).

## This PR (PR1 — integration records)

`IVcsIntegrationStore` (+ both impls + migration); `ListAllVcsIntegrations` and per-kind
`List`/`Get`/`Update`/`Delete` for `github`, `github-enterprise`, `azure-devops`; console VCS
list + disconnect wired to the real endpoints. The design doc lands with this PR.
