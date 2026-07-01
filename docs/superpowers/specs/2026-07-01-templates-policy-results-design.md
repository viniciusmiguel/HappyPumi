# Templates + Policy results — design

> Status: approved (brainstorm) · 2026-07-01 · phased across 2 PRs

## Goal

Implement two org-admin surfaces: **templates** (org-registered template sources + project-template
resolution) and **policy results** (CrossGuard compliance views + policy groups), plus the small
`UpdateAuthPolicy` loose end. **16 currently-`NotImplementedException` endpoints**. "Real where it
counts": policy results genuinely aggregate over the existing policy findings; policy groups read/mutate
the existing policy store; template sources are records with URL validation; project-template resolves
from the existing template registry.

## Decisions (from brainstorm)

- **Scope = 16 endpoints:** 7 templates (4 source CRUD + 3 project-template) + 8 policy (4 results + 3
  groups + 1 registry policy-pack) + `UpdateAuthPolicy`.
- **Exclude** the `/api/preview/registry/*` policypack + template endpoints (Registry-preview product)
  and `/api/preview/insights/*` policy endpoints (Insights product) — each is its own deferred group.
- **Real where it counts** (aggregation over existing stores), not records-only.
- **2 phased PRs**, each merged when CI is green (same delivery loop as prior groups).

## Existing building blocks

- `ITemplateRegistry` / `RegistryMapper` / `StoredTemplateVersion` — the published-template registry
  (used by the Registry group). Project-template resolution reuses it.
- `IPolicyStore` — `ListGroups(org)` / `GetGroup(org,name)` / `NewGroup(org,name)` / `ListPacks(org)` /
  `GetPack(org,name)` (+ `StoredPolicyGroup` / `StoredPolicyPack`). Needs a new `UpdateGroup` for batch
  pack-assignment.
- `IPolicyFindingStore` — recorded policy violations (events → findings). Policy-results aggregation
  reads it; no new domain store.
- `GetAuthPolicy` (shipped in settings PR3) currently **synthesizes** a default `AuthPolicy` (no store).

## Architecture

### Templates (PR1)
- **`ITemplateSourceStore`** (ADR-0005: interface in `State/`, `InMemory*` + `Postgres*`, EF migration,
  DI): `StoredTemplateSource { Id, Org, Name, SourceUrl, DestinationUrl?, IsValid, Error? }`. Methods:
  `Create`, `List(org)`, `Get(org,id)`, `Update(org,id, mutate)`, `Delete(org,id)`.
- **Source endpoints** (`/api/orgs/{org}/templates/sources`): `GetOrgTemplateCollections` (list →
  `GetOrgTemplateSourcesResponse`), `CreateOrgTemplateCollection` (→ `TemplateSource`),
  `UpdateOrgTemplateCollection` (PATCH `/{templateID}` → `TemplateSource`), `DeleteOrgTemplateCollection`
  (DELETE `/{templateID}` → 204/404). On create/update, a lightweight validation sets `IsValid`/`Error`
  (well-formed URL check; no network fetch required — keep it deterministic for tests).
- **Project-template endpoints**: `GetProjectTemplate` (`/api/orgs/{org}/template` → the resolved
  template object), `GetProjectTemplateConfiguration` (`/template/configuration` →
  `GetTemplateConfigurationResponse`), `GetOrgTemplateReadme` (`/template/readme` → README string).
  Resolve the requested template from `ITemplateRegistry` (reuse `RegistryMapper`); 404 when absent.
- Console: an org **Templates** settings page (list/add/edit/delete sources) + a browse of resolvable
  project templates.

### Policy results & groups + auth policy (PR2)
- **`PolicyResultsAggregator`** (owned service over `IPolicyFindingStore`): computes
  `GetPolicyResultsMetadata` (`{ policyTotalCount, policyWithIssuesCount, resourcesTotalCount,
  resourcesWithIssuesCount }`), `GetPolicyIssuesFilters` (distinct `{ field, values }` for the filter
  UI), `ListPoliciesCompliance` (`{ policies, totalCount, continuationToken }` — per-policy
  pass/violation rollup), and `ExportPolicyIssues` (CSV/JSON string of the issues).
- **Policy-groups endpoints**: `GetPolicyGroupMetadata` (`/api/orgs/{org}/policygroups/metadata` →
  `PolicyGroupMetadata` counts), `BatchUpdatePolicyGroup` (PATCH `/policygroups/{group}/batch` — add a
  new `IPolicyStore.UpdateGroup` to apply pack add/remove), `GetStackPolicyGroups`
  (`/api/stacks/{org}/{proj}/{stack}/policygroups` → `AppListPolicyGroupsResponse`).
- **`GetOrgRegistryPolicyPack`** (`/api/orgs/{org}/registry/policypacks/{name}`) →
  `GetRegistryPolicyPackVersionResponse` from `IPolicyStore.GetPack`.
- **Auth policy**: a small **`IAuthPolicyStore`** (ADR-0005) keyed by `(org, policyId)` storing an
  `AuthPolicy`. `UpdateAuthPolicy` (PATCH `/api/orgs/{org}/auth/policies/{policyId}`) persists the
  update and returns it; `GetAuthPolicyEndpoint` is rewired to read the store, falling back to the
  current synthesized default when absent (a minimal edit to that already-implemented endpoint).
- Console: a **Policy results** page (metadata cards + compliance table + filters + export) and policy
  groups surfaced on the org policy page.

## Auth
Keep `AllowAnonymous()` to match sibling implemented org/stack endpoints (the standing permissive-token
decision).

## Testing & process
- Stores → in-memory unit tests; `PolicyResultsAggregator` → unit tests over a seeded
  `IPolicyFindingStore`; endpoints → component tests on real Postgres
  (`[Collection(HappyPumiCollection.Name)]`, `app.CreateClient()`).
- Templates: source CRUD round-trip + validation; project-template resolves a registered template (404
  when missing).
- Policy: seed findings → metadata counts / filters / compliance / export reflect them; batch-update a
  group's packs → `GetGroup` reflects it; `GetStackPolicyGroups`; `GetOrgRegistryPolicyPack` (404 when
  absent). Auth policy: update → get reflects it; get defaults when never set.
- Console `npm run build && npm run lint` per PR. Coverage > 80% changed C#, duplication < 3%.
- Each PR branches off `main`, granular commits (`Co-Authored-By` trailer), merged when CI green. No
  squash. Edit only `HandleAsync` bodies in `// <auto-generated />` endpoint files; never contract
  files. The recursive-`Body` / polymorphic-discriminator quirks may appear — use the `Contracts.X` /
  `EndpointWithoutRequest` workaround (or the existing `ChangeGateJson`-style sanitizer) and report.
- The unrelated pre-existing OIDC/Entra WIP is set aside (stashed + test moved) for the run and restored
  at the end.

## Decomposition (2 PRs)

| PR | Scope | Endpoints |
|---|---|---|
| 1 | **Templates** — `ITemplateSourceStore` (+migration) + 4 source endpoints + 3 project-template endpoints (resolve via `ITemplateRegistry`) + console templates page | `GetOrgTemplateCollections`, `CreateOrgTemplateCollection`, `UpdateOrgTemplateCollection`, `DeleteOrgTemplateCollection`, `GetProjectTemplate`, `GetProjectTemplateConfiguration`, `GetOrgTemplateReadme` |
| 2 | **Policy results & groups + auth policy** — `PolicyResultsAggregator` over `IPolicyFindingStore`, `IPolicyStore.UpdateGroup`, `IAuthPolicyStore` (+migration), rewire `GetAuthPolicy`, console policy-results page | `GetPolicyResultsMetadata`, `GetPolicyIssuesFilters`, `ExportPolicyIssues`, `ListPoliciesCompliance`, `GetPolicyGroupMetadata`, `BatchUpdatePolicyGroup`, `GetStackPolicyGroups`, `GetOrgRegistryPolicyPack`, `UpdateAuthPolicy` |

## Acceptance
Every listed endpoint returns real data / performs its action (no `NotImplementedException`), covered by
tests; policy-results views are computed from actual findings; policy-group batch updates persist;
template sources round-trip with validation and project-templates resolve from the registry; auth-policy
updates persist and are reflected by get. Console pages build + lint clean.

## This PR (PR1 — Templates)
`ITemplateSourceStore` (+ both impls + migration); the 4 source endpoints + 3 project-template
endpoints; the console templates page. The design doc lands with this PR.
