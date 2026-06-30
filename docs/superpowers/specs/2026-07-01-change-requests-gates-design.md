# Change requests, gates & approvals — design

> Status: approved (brainstorm) · 2026-07-01 · phased across 3 PRs

## Goal

Implement HappyPumi's **change-request** workflow surface — a PR-like review flow for **ESC
environments** — and the **change gates** that govern it. **17 currently-`NotImplementedException`
endpoints** (8 change-request actions + 5 change-gate CRUD + 4 ESC `/api/preview/...` draft
endpoints), plus **reclaiming** the 2 already-implemented `Approve`/`Unapprove` endpoints. The flow is
**full-real**: a change request wraps an ESC env
draft, moves through a status lifecycle, accrues an event timeline, is gated by approval-required
gates, and **apply commits the draft as a new env revision**.

## Decisions (from brainstorm)

- **Full real** workflow (not records-only): apply genuinely commits the draft revision and gates
  genuinely block apply until satisfied.
- **Target scope is ESC environments only** — the generated `TargetEntity` contract models only the
  `environment` discriminator; we do not invent stack-update change requests (that would require
  editing contracts, which the generator owns).
- **Reclaim `Approve`/`Unapprove`** (`/api/change-requests/{org}/{id}/approve`) for real change
  requests, using **dispatch-by-ID**: look up a change request by ID first → approve the CR; if no CR
  has that ID, fall back to the existing ESC open-request grant logic **unchanged**. Zero risk to the
  merged ESC secret-reveal feature and its tests; no invented routes.
- **Include the 4 ESC draft-preview endpoints** — they are the draft content behind an env-targeted
  change request; including them makes the env change flow end-to-end.
- **3 phased PRs**, each merged when CI is green (same delivery loop as the settings cluster).

## Key existing facts

- The **draft ID _is_ the change-request ID**. The `/api/esc/{org}/{proj}/{env}/drafts...` endpoints
  (Create/Open/Read/Update) are **already implemented** (`IEscDraftStore`, `EscDraft(Id, Yaml,
  BaseRevision)`). The `/api/preview/esc/.../drafts...` routes are the duplicate aliases and are the
  stubs in scope.
- The existing `IApprovalRuleStore` + `EscOpenGate` (ESC secret-reveal gating, `ApprovalRuleRow
  {StackPattern, RequiredApprovals}`) is a primitive precursor of change gates. It **stays untouched**;
  change gates are the new first-class model.
- Rich contracts already exist (generated): `ChangeRequest`, `GetChangeRequestResponse`
  (`: ChangeRequest` + `gateEvaluation`), `ListChangeRequestsResponse` (+`continuationToken`),
  `ChangeRequestEvent` (polymorphic on `eventType`: `approved_by_user`, `commented`,
  `description_updated`, `revision_added`, `status_changed`, `unapproved_by_user`),
  `ChangeRequestApplyResult {entityUrl, message}`, `TargetEntity` (polymorphic, only `environment`),
  `ChangeGate {enabled, id, name, rule, target}`, `ChangeGateRuleOutput`/`Input` (polymorphic,
  `approval_required` → `ChangeGateApprovalRuleOutput/Input {allowSelfApproval, eligibleApprovers,
  numApprovalsRequired, requireReapprovalOnChange}`), `ChangeGateTargetOutput/Input {actionTypes,
  entityType, qualifiedName}`, `CreateChangeGateRequest`, `UpdateChangeGateRequest`,
  `UpdateChangeRequestRequest {description}`, `SubmitChangeRequestRequest {description?}`,
  `AddChangeRequestCommentRequest {comment}`, `ListChangeGatesResponse`,
  `ListChangeRequestEventsResponse`.

## Architecture

Three new persistence seams (ADR-0005: interface in `State/`, `InMemory*` + `Postgres*` impls, EF
migration, DI in `Program.cs`). The existing ESC draft/revision machinery is reused for content + apply.

- **`IChangeRequestStore`** — CR workflow records:
  `StoredChangeRequest { Id, Org, Action, Description, TargetProject, TargetEnv, Status,
  LatestRevisionNumber, CreatedBy, CreatedAt, Approvers: List<string> }`. Status ∈
  `draft | submitted | applied | closed`.
  Methods: `Create`, `Get(org,id)`, `List(org)`, `Update(org,id,…)` (description / status /
  approvers / revision), `Delete` (not exposed — CRs close, not delete).
- **`IChangeRequestEventStore`** — append-only timeline:
  `StoredChangeRequestEvent { Id, ChangeRequestId, Org, EventType, Comment?, RevisionNumber,
  CreatedBy, CreatedAt }`. Methods: `Append`, `List(org, changeRequestId)`.
- **`IChangeGateStore`** — first-class gates:
  `StoredChangeGate { Id, Org, Name, Enabled, RuleType, NumApprovalsRequired, AllowSelfApproval,
  RequireReapprovalOnChange, EligibleApprovers: List<string>, TargetEntityType, ActionTypes:
  List<string>, QualifiedName? }`. Methods: `Create`, `Get(org,id)`, `List(org)`, `Update`, `Delete`.
- **`ChangeGateEvaluator`** (owned service) — evaluates a CR against matching **enabled** gates:
  target `entityType == "environment"`, the CR's `Action ∈ ActionTypes`, and `QualifiedName` glob
  (reuse the `EscOpenGate` `*`-glob helper) over `"{project}/{env}"`. For each `approval_required`
  rule: count distinct approvers (exclude the creator unless `allowSelfApproval`); the rule is
  satisfied when count ≥ `numApprovalsRequired`. Produces a `ChangeRequestGateEvaluation` (surfaced
  on `GetChangeRequest`) and a boolean "apply allowed". `requireReapprovalOnChange`: adding a new
  revision after approval clears `Approvers`.

### Mappers
`ChangeRequestMapper` (`StoredChangeRequest` → `ChangeRequest` / `GetChangeRequestResponse`, building
`TargetEntityEnvironment`), `ChangeRequestEventMapper` (→ the polymorphic event subtypes by
`EventType`), `ChangeGateMapper` (`StoredChangeGate` ↔ the rule/target input+output contracts).

## Endpoints (16) — edit only `HandleAsync` + ctor injection; never contract files

**Change requests** (`/api/change-requests/{orgName}/...`, in `Organizations/`):
- `ListChangeRequests` GET → `ListChangeRequestsResponse` (`continuationToken` empty for now).
- `Get` GET `/{id}` → `GetChangeRequestResponse` (CR + `gateEvaluation`); 404 if missing.
- `Update` PATCH `/{id}` → set description; append `description_updated`; return updated CR.
- `Submit` POST `/{id}/submit` → status `draft`→`submitted`; optional description; append
  `status_changed`.
- `Apply` POST `/{id}/apply` → **gate-enforced**: if any matching gate unsatisfied → 400 with the
  blocking reason; else commit the draft YAML as a new env revision (reuse the existing draft→revision
  path), status→`applied`, `LatestRevisionNumber`++, append `revision_added` + `status_changed`,
  return `ChangeRequestApplyResult {entityUrl, message}`.
- `Close` POST `/{id}/close` → status→`closed`; append `status_changed`.
- `AddComment` POST `/{id}/comments` → append a `commented` event (`AddChangeRequestCommentRequest`).
- `ListEvents` GET `/{id}/events` → `ListChangeRequestEventsResponse`.
- `Approve` POST `/{id}/approve` + `Unapprove` DELETE `/{id}/approve` (**already implemented for ESC
  grants** — extend, dispatch-by-ID): CR found → add/remove approver, append
  `approved_by_user`/`unapproved_by_user`, honor `allowSelfApproval`/separation-of-duties, re-evaluate
  gates; CR not found → existing ESC open-request grant logic unchanged.

**Change gates** (`/api/change-gates/{orgName}`, in `Organizations/`):
- `CreateGate` POST → `ChangeGate`; `ListGates` GET → `ListChangeGatesResponse`; `ReadGate` GET
  `/{gateID}` → `ChangeGate` (404); `UpdateGate` PUT `/{gateID}` → `ChangeGate` (404); `DeleteGate`
  DELETE `/{gateID}` → 204/404. All audited (ADR-0010).

**ESC draft-preview** (`/api/preview/esc/.../drafts...`): `Create`, `Open`, `Read`, `Update` —
delegate to the same logic as the implemented `/api/esc/.../drafts` siblings (the CR/draft share an ID).

### CR creation
A CR is created at **draft creation**: `CreateEnvironmentDraft` (already implemented) gains a small
hook to register a `StoredChangeRequest` (status `draft`, action `update`, target = the env) keyed by
the draft ID. No separate Create endpoint exists in the spec, consistent with this.

## Auth
Keep `AllowAnonymous()` to match the sibling implemented endpoints (`Approve`/`Unapprove` and the rest
of the org/console surfaces); token enforcement stays permissive (the standing cluster decision).

## Frontend
A console **Change requests** review surface: a list (status, target, creator), a detail view with the
draft diff, the event timeline, approve/unapprove, comment, submit/apply/close actions, and the gate
evaluation status; plus a **Change gates** settings page (list/create/edit/delete approval gates).
Reuse `Card`/`Table`/`Modal`/`Field`/`Badge`; new `api.*` fetchers; wire into routing/nav.

## Testing & process
- Stores → in-memory unit tests; endpoints → component tests on real Postgres
  (`[Collection(HappyPumiCollection.Name)]`, `app.CreateClient()`).
- CR lifecycle (create draft → list/get → update/submit/comment → timeline reflects each).
- Gate enforcement (gate matching the env → submit → Apply refused → approve to threshold → Apply
  commits a revision, status `applied`).
- Separation-of-duties (creator can't approve unless `allowSelfApproval`).
- Approve dispatch regression: a CR ID approves the CR; a non-CR ID still hits the ESC-grant path.
- Gates CRUD; the 4 preview-draft endpoints round-trip like their `/api/esc/` siblings.
- Console `npm run build && npm run lint` per PR. Coverage > 80% changed C#, duplication < 3%.
- Each PR branches off `main`, granular commits (`Co-Authored-By` trailer), merged when CI green. No
  squash. Edit only `HandleAsync` bodies in `// <auto-generated />` endpoint files; never contract
  files. The recursive-`Body` quirk may appear on un-touched stubs — use the `Contracts.X` /
  `EndpointWithoutRequest` workaround and report.
- The unrelated pre-existing OIDC/Entra WIP is set aside (stashed + test moved) for the run and
  restored at the end.

## Decomposition (3 PRs)

| PR | Scope | Endpoints |
|---|---|---|
| 1 | **Change gates** — `IChangeGateStore` (+migration) + mapper + 5 gate endpoints + console gates page | `CreateGate`, `ListGates`, `ReadGate`, `UpdateGate`, `DeleteGate` |
| 2 | **Change requests core** — `IChangeRequestStore` + `IChangeRequestEventStore` (+migrations), CR-creation hook in `CreateEnvironmentDraft`, the 8 CR endpoints (Apply = simple draft-revision commit, no gate enforcement yet), reclaimed `Approve`/`Unapprove` (dispatch-by-ID) | `ListChangeRequests`, `Get`, `Update`, `Submit`, `Apply`, `Close`, `AddComment`, `ListEvents`, `Approve`, `Unapprove` |
| 3 | **Gate enforcement + ESC draft-preview + console** — `ChangeGateEvaluator` wired into `gateEvaluation` + Apply refusal + `requireReapprovalOnChange`, the 4 preview-draft endpoints, console CR review page | `CreateEnvironmentDraftPreview`, `OpenEnvironmentDraftPreview`, `ReadEnvironmentDraftPreview`, `UpdateEnvironmentDraftPreview` |

## Acceptance
Every listed endpoint returns real data / performs its action (no `NotImplementedException`), covered
by tests; a change gate genuinely blocks `Apply` until approvals reach the threshold; `Apply` commits
a new env revision; `Approve`/`Unapprove` operate on change requests while the ESC secret-reveal grant
path still works; the console manages change requests and gates and builds + lints clean.

## This PR (PR1 — change gates)
`IChangeGateStore` (+ both impls + migration); `ChangeGateMapper`; the 5 change-gate endpoints; the
console change-gates page. The design doc lands with this PR.
