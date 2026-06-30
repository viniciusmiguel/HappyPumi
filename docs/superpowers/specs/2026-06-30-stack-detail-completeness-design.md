# Stack Detail — feature-completeness design

> Status: approved (brainstorm) · 2026-06-30 · phased across 6 PRs

## Goal

Bring the console **Stack Detail** page to feature parity with the real Pulumi Cloud stack
detail: implement the ~26 stack-scoped API endpoints that are currently
`NotImplementedException` stubs, and build the missing console UI (tabs, drill-in pages,
components) on top of them.

## Decisions (from brainstorm)

- **Phased:** one spec, six PRs — each implements one sub-feature end-to-end (TDD backend →
  component tests → UI → wire-up → PR off `main`).
- **Persist engine events:** add an engine-events store and wire the existing
  `RecordEngineEvent_*` endpoints to persist, enabling a real update timeline + resource diff
  (rather than best-effort derived data).

## Scope

In scope: the stack-scoped endpoints below. **Excluded** (other pages): `GetOrganization`,
`UpdateOrganizationSettings`, `TransferAllStacks` (org settings); `GetStackPolicyGroups`
(Policy page).

| PR | Sub-feature | Endpoints | New / enriched UI |
|---|---|---|---|
| 1 | Resource detail | `GetStackResources` (by version), `GetStackResource` (single@version), `GetLatestStackResource`, `GetStackOverview` | Resource detail drawer (inputs/outputs/deps/parent); per-version resource view; richer Overview |
| 2 | Update + preview detail | engine-events store + wire `RecordEngineEvent[Batch]_*`; `UpdateSummary`, `UpdateSummaryHandlerLatest`, `GetStackUpdateSummary`, `GetUpdateTimeline`, `GetLatestUpdateTimeline`, `GetStackPreview`, `GetStackPreviews`, `GetStackPreviewSummary`, `GetLatestStackPreviews` | Update detail page (summary, resource diff, step timeline); preview history |
| 3 | Activity | `GetStackActivity` | Activity tab |
| 4 | Access / collaborators | `ListStackPermissions`, `DeleteStackPermission`, `ListMemberStackPermissions`, `ListStackTeams`, `UpdateTeamStackPermissions` | Access tab (collaborators + team perms; add/remove) |
| 5 | Stack references | `ListUpstreamStackReferences`, `ListDownstreamStackReferences` | References section (depends-on / depended-by) |
| 6 | Settings actions | `TransferStack`, `ReassignStackOwnership`, `UpdateStackNotificationSettings`, `UpdateStackTag`, `GetStacksAnnotation`, `UpsertStacksAnnotations`, `GetStackStarterWorkflow`, `ExportStackAtVersion` | Settings tab actions (transfer, owner, notifications, single-tag edit, export@version) |

## Backend architecture

Each endpoint maps to a concrete data source. New persistence seams follow the existing
ADR-0005 pattern: an interface in `State/` with an `InMemory*` default and a `Postgres*`
implementation (EF Core + `jsonb`), registered in DI.

- **Per-version resources** — `version → updateId` via the stack history entry
  (`StoredHistoryEntry.Info.Version`), then `StoredUpdate.Checkpoint` → resources. Add
  `IUpdateStore.FindByVersion(StackCoordinates, long)`. Latest resource(s) come from the
  current `StoredStack.Deployment`. A single resource is located by URN within that checkpoint.
- **`GetStackOverview`** — read-side aggregation (latest update, resource count by type,
  last-update summary, tags) over existing stores; no new persistence.
- **Engine events (new seam `IEngineEventStore`)** — append engine events per update
  (`jsonb` list). `RecordEngineEvent_*` and `RecordEngineEventBatch_*` (all four kinds)
  persist instead of dropping. The update **timeline** is the ordered event stream folded
  into per-resource steps; the update **summary/diff** is create/update/delete/same counts
  derived from `ResourcePreEvent` + `ResOutputsEvent`.
- **Previews** — preview updates already flow through the lifecycle with `DryRun=true`; their
  persisted events + checkpoint back the preview summary and preview history (the same
  machinery as updates, filtered to dry-runs).
- **Activity** — a read-side `StackActivity` projection that merges the stack's update history,
  deployments, and config/tag mutations into one time-ordered feed. No new store.
- **Stack permissions (new seam `IStackPermissionStore`)** — maps a user or team to a role on
  a specific stack; backs collaborators list/delete, member-stack-permission lookup, list
  teams, and update-team-stack-permissions.
- **Stack references** — derived: scan the org's stacks' checkpoints for `StackReference`
  resources. Upstream = stacks this stack reads; downstream = stacks that read this one.
  Read-side, no new store.
- **Annotations (new seam `IStackAnnotationStore`)** — keyed by (stack, kind), `jsonb` payload;
  backs get/upsert annotations.
- **Settings actions** — extend `IStackStore`: `Transfer` (move to a new org), `SetOwner`,
  `SetNotificationSettings`; single-tag update reuses `SetTag`. `ExportStackAtVersion` returns
  the checkpoint at a given version (via `FindByVersion`).

## Frontend architecture

`StackDetail.tsx` (342 lines) would exceed the 500-line limit with three new tabs and two
drill-in pages, so it is split:

- `StackDetail.tsx` → thin shell: header, breadcrumb, tab router, shared stack-context fetch.
- Each tab → `console/src/pages/stack/{Overview,Updates,Resources,Activity,Access,References,Settings}.tsx`.
- New drill-in pages: `console/src/pages/stack/UpdateDetail.tsx`, `console/src/pages/stack/ResourceDetail.tsx`
  (reached from the Updates and Resources tabs respectively).
- New API methods grouped in `console/src/lib/api.ts`; if it crosses ~500 lines, split a
  `console/src/lib/api/stack.ts`.
- New tabs: **Activity, Access, References**. Existing tabs (Overview, Updates, Resources,
  Settings) are enriched in place, not replaced. Uses the existing component library
  (`Tabs`, `Table`, `Card`, `KeyValue`, `Badge`, `StatusDot`).

## Testing & process

- **Backend:** TDD — a failing component test (`HappyPumi.Api.Tests`, in-process
  `WebApplicationFactory`) for each endpoint's contract + behavior, then implement. New stores
  get unit tests for their in-memory implementation.
- **Console:** not in CI — `cd console && npm run build && npm run lint` locally per PR.
- **Gates:** coverage > 80% on changed C#, duplication < 3%, lint clean — checked locally
  before each PR.
- **Git:** each PR is a branch off `main` → granular commits (Co-Authored-By trailer) → green
  CI → `gh pr merge --merge --delete-branch`. No squash.
- **Generated files:** stub endpoint bodies are `// <auto-generated />`; edit only the
  `HandleAsync` body (the established pattern), never the contract files, mindful of the
  generator-overwrite caveat (HappyPumi/CLAUDE.md).

## Acceptance (per sub-feature)

- Every listed endpoint returns real data (no `NotImplementedException`), covered by a
  component test; the matching console surface renders that data and the console builds + lints.
- After all six PRs: the Stack Detail page has Overview, Readme, Updates (+ update detail),
  Resources (+ resource detail), Activity, Access, References, Deployments, and Settings, each
  backed by a real endpoint.

## This PR (PR1 — Resource detail)

`GetStackResources`, `GetStackResource`, `GetLatestStackResource`, `GetStackOverview` +
`IUpdateStore.FindByVersion`; console resource detail drawer + per-version resource view +
enriched Overview. The design doc lands with this PR.
