# Org-admin long-tail cluster — design

> Status: approved (brainstorm) · 2026-07-01 · phased across 6 PRs

## Goal

Implement the org-admin long-tail — **~43 `NotImplementedException` endpoints** across 8 cohesive
sub-areas — as a phased cluster over mostly-existing stores plus a few small new ones (ADR-0005).
"Real where a store exists": audit/roles/agent-pools/stacks operate on real data; new stores hold real
records; aggregations without a backing data source return **deterministic zeros/empties** (never
fabricated, logged when a source is absent).

## Decisions (from brainstorm)

- **Full cluster in scope** — all 4 sub-area groups (org core/members/roles, audit query+export,
  services+agent-pools, and search/usage/stack-restore/user-account).
- **Real where a store exists**; deterministic zero/empty aggregations where none does.
- **6 phased PRs**, each merged when CI is green (same delivery loop as prior clusters).

## Existing building blocks

- `IAuditLog` — `Record(org,event,description,actor)` + `List(org) -> IReadOnlyList<AuditLogRow>`
  (audit query/export build on this).
- `IIdentityStore` — members (`UpdateMemberRole`) + roles (`ListRoles`/`GetRole`/`CreateRole`/
  `UpdateRole`/`DeleteRole`/team-role assignment). Members/roles endpoints extend this.
- `IServiceStore` (Postgres, "IDP services") — services catalog builds on this (extended for items).
- `IAgentPoolStore` — Create/Get/List agent-pool endpoints already implemented; Delete/Patch remain.
- `IStackStore` — stack restore/transfer build on this (soft-delete tracking added).
- `Contracts/Organization.cs` — the org wire shape returned by `GetOrganization`.
- No resource store exists → resource-search / discovered-resource endpoints return deterministic
  empty result sets / zero aggregations.

## Sub-area architecture

- **Org core, members & roles** — new `IOrgSettingsStore` `{ Org, DisplayName?, DefaultRepo?, ... }`
  backs `GetOrganization` (synthesizes the `Organization` contract, members from `IIdentityStore`) and
  `UpdateOrganizationSettings`. `ListUsersWithRole` / `ListAvailableScopes` (from `RbacPermissions`) /
  `UpdateOrganizationDefaultRole` (a default-role flag) / `SetSoleOrganizationAdmin` extend
  `IIdentityStore` (add list-members-by-role + default-role flag + set-admin).
- **Audit log query & export** — `ListAuditLogEventsHandlerV2` + `ExportAuditLogEventsHandlerV1/V2`
  (CSV) computed from `IAuditLog.List`; `GetAuditLogsReaderKind` returns the configured reader kind; a
  new `IAuditExportConfigStore` backs `Get/Update/DeleteAuditLogExportConfiguration` +
  `ForceAuditLogExport` (records a forced-export marker) + `TestAuditLogExportConfiguration` (validates
  the config shape, no external call).
- **Services catalog + agent pools** — `GetService`/`UpdateService`/`DeleteService` +
  `AddServiceItems`/`RemoveServiceItem` (+ `HeadService`) over `IServiceStore` (extended with a
  service-items collection); `DeleteOrgAgentPool`/`PatchOrgAgentPool` over `IAgentPoolStore` (add
  Delete/Update).
- **Resource search & dashboard + usage summaries** — `GetResourceColumnFilterSet`,
  `GetResourceDashboardAggregations`, `ExportOrgResourceSearchQuery`, `GetOrgResourceSearchV2Query`,
  `SearchClusterAvailable` return deterministic empty/zero results (no resource store); usage summaries
  (`GetPackageUsedByStacks` from the package registry; `GetUsageSummaryEnvironmentSecrets` from ESC
  envs; `GetUsageSummaryDiscoveredResourceHours` → zeros).
- **Stack restore & bulk transfer** — `ListDeletedStacks`/`RestoreDeletedStack` over a soft-delete
  marker on `IStackStore` (deletes tombstone rather than hard-delete; restore un-tombstones);
  `TransferAllStacks` reassigns stacks to a target org.
- **User account (`/api/user/*`)** — new small `IUserAccountStore` backs pending email changes
  (`Get/DeletePendingEmailChange`), `GetUserHasVerifiedEmail`, `ListUserOrgInvites`,
  `UpdateDefaultOrganization`; the VCS identity-provider endpoints (`DeleteIdentityProvider`,
  `ListIdentityProviderOrganizations`, `SyncWithIdentityProvider`, `GetGroupsForGitLabApp`) return
  config-gated records/empties consistent with the VCS integration seam (ADR-0009).

## Auth
Keep `AllowAnonymous()` to match sibling implemented org/user endpoints (the standing permissive-token
decision).

## Frontend
Console surfaces where they add value: an org **Settings** page (name/default-role/members-by-role +
set-admin), an **Audit log** page (v2 list + export + export-config), a **Services** catalog page, and
a **Deleted stacks** (restore) view. Search/usage/user-account get minimal or no console (API-first;
add cards only where a real value exists). Reuse `Card`/`Table`/`Modal`/`Field`/`Badge`.

## Testing & process
- Stores → in-memory unit tests; endpoints → component tests on real Postgres
  (`[Collection(HappyPumiCollection.Name)]`, `app.CreateClient()`).
- Audit: seed events via `IAuditLog.Record` → v2 list/export reflect them; export-config round-trips.
- Members/roles: seed roles/members → list-by-role, default-role, set-admin, scopes.
- Services: create (existing) → get/update/delete + item add/remove round-trip.
- Agent pools: delete/patch over a seeded pool.
- Stack restore: soft-delete a stack → appears in deleted list → restore → back in the live list.
- Search/usage: return 200 with deterministic empty/zero shapes (never 500).
- User account: pending-email/invite/verified-email/default-org round-trips.
- Console `npm run build && npm run lint` per PR. Coverage > 80% changed C#, duplication < 3%.
- Each PR branches off `main`, granular commits (`Co-Authored-By` trailer), merged when CI green. No
  squash. Edit only `HandleAsync` bodies in `// <auto-generated />` endpoint files; never contract
  files. The recursive-`Body` / polymorphic-discriminator quirks may appear — use the `Contracts.X` /
  `EndpointWithoutRequest` workaround (or the existing `ChangeGateJson` sanitizer) and report.
- The unrelated pre-existing OIDC/Entra WIP is set aside (stashed + test moved) for the run and restored
  at the end.

## Decomposition (6 PRs)

| PR | Sub-area | Endpoints |
|---|---|---|
| 1 | **Org core, members & roles** — `IOrgSettingsStore` + `IIdentityStore` extensions | `GetOrganization`, `UpdateOrganizationSettings`, `SetSoleOrganizationAdmin`, `ListUsersWithRole`, `ListAvailableScopes`, `UpdateOrganizationDefaultRole` |
| 2 | **Audit log query & export** — over `IAuditLog` + `IAuditExportConfigStore` | `ListAuditLogEventsHandlerV2`, `ExportAuditLogEventsHandlerV1`, `ExportAuditLogEventsHandlerV2`, `GetAuditLogsReaderKind`, `GetAuditLogExportConfiguration`, `UpdateAuditLogExportConfiguration`, `DeleteAuditLogExportConfiguration`, `ForceAuditLogExport`, `TestAuditLogExportConfiguration` |
| 3 | **Services catalog + agent pools** — over `IServiceStore` (extended) + `IAgentPoolStore` | `GetService`, `UpdateService`, `DeleteService`, `AddServiceItems`, `RemoveServiceItem`, `HeadService`, `DeleteOrgAgentPool`, `PatchOrgAgentPool` |
| 4 | **Resource search & usage summaries** — deterministic empties/zeros + registry/ESC-derived usage | `GetResourceColumnFilterSet`, `GetResourceDashboardAggregations`, `ExportOrgResourceSearchQuery`, `GetOrgResourceSearchV2Query`, `SearchClusterAvailable`, `GetPackageUsedByStacks`, `GetUsageSummaryEnvironmentSecrets`, `GetUsageSummaryDiscoveredResourceHours` |
| 5 | **Stack restore & bulk transfer** — soft-delete over `IStackStore` | `ListDeletedStacks`, `RestoreDeletedStack`, `TransferAllStacks` |
| 6 | **User account** — new `IUserAccountStore` + VCS identity-provider (ADR-0009) | `GetLatestPendingEmailChange`, `DeletePendingEmailChange`, `GetUserHasVerifiedEmail`, `ListUserOrgInvites`, `UpdateDefaultOrganization`, `DeleteIdentityProvider`, `ListIdentityProviderOrganizations`, `SyncWithIdentityProvider`, `GetGroupsForGitLabApp` |

## Acceptance
Every listed endpoint returns real data / performs its action (no `NotImplementedException`), covered by
tests; audit query/export reflect real recorded events; members/roles/agent-pools/stack-restore operate
on real stores; new stores round-trip; aggregations without a data source return deterministic zeros/
empties (200, never 500). Console pages build + lint clean.

## This PR (PR1 — Org core, members & roles)
`IOrgSettingsStore` (+ both impls + migration); `IIdentityStore` extensions (list-members-by-role,
default-role flag, set-admin); the 6 endpoints; the org Settings console page. The design doc lands with
this PR.
