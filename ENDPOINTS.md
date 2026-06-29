# HappyPumi — API Endpoint Implementation Priority

> Reverse-engineering roadmap for re-implementing the Pulumi Cloud API in `HappyPumi.Api`.
> Ordered by **importance for getting basic Pulumi IaC + IDP functionality working**.

> **Clean-room note:** This roadmap and HappyPumi as a whole are a clean-room implementation derived **only**
> from the public Pulumi Cloud OpenAPI spec and the **Apache-2.0-licensed** Pulumi CLI. No proprietary Pulumi
> Cloud server source is used. See ADR-0008 (`docs/adr/0008-clean-room-implementation.md`).

## How this was derived

Two **public** sources were cross-referenced:

1. **`pulumi-spec.json`** — the full Pulumi Cloud OpenAPI spec: **588 operations** across 14 tags.
2. **The Pulumi CLI's HTTP client** (`pulumi/pkg/backend/httpstate/client/`, Apache-2.0) — the *authoritative*
   list of what a real `pulumi` CLI actually calls against the service.

Of the 588 spec operations, **only ~161 are ever invoked by the open-source CLI**. The other ~427 power the
web console, ESC (Environments), Insights scanning, AI/Neo, and other Pulumi Cloud product surfaces that are
**not required** for `pulumi login / up / preview / refresh / destroy` to work. Those are deliberately pushed
to the bottom tiers so reverse-engineering effort goes where it actually unblocks the IaC workflow first.

Each row lists the **operationId**, which is also the HappyPumi endpoint class/file name
(e.g. `CreateUpdateForUpdate` → `HappyPumi.Api/Stacks/CreateUpdateForUpdateEndpoint.cs`). Every endpoint is
already scaffolded with FastEndpoints; the work is replacing the `throw new NotImplementedException(...)` body.

**Legend:** ✅ = called by the CLI (verified in client source) · 🌐 = console/other-product only (not CLI).

---

## Tier 0 — Bootstrap & Identity (do first; nothing works without these)

`pulumi login` and every subsequent call depend on these. They are tiny and unblock all client traffic.

| ✅/🌐 | Method | Path | operationId |
|---|---|---|---|
| ✅ | GET  | `/api/user`                          | `GetCurrentUser` |
| ✅ | GET  | `/api/user/organizations/default`    | `GetDefaultOrganization` |
| ✅ | GET  | `/api/cli/version`                   | `Version` |
| ✅ | GET  | `/api/capabilities`                  | `Capabilities` |
| ✅ | GET  | `/api/openapi/pulumi-spec.json`      | `FetchRestSpecification` |
| ✅ | POST | `/api/oauth/token`                   | `Token` (OIDC token exchange — needed for CI/OIDC login) |

**Why first:** `GetPulumiAccountDetails` (`/api/user`) is the call the CLI makes to validate a token on
`pulumi login`. `GetDefaultOrganization` resolves the org for unqualified stack names. `Capabilities` /
`Version` gate feature negotiation (delta checkpoints, journaling, etc.) — return sane defaults early.

---

## Tier 1 — Core IaC State Engine (the heart of `pulumi up/preview/refresh/destroy`)

This is the single most important block. Without it there is no IaC. Implement in the sub-order below.

### 1a. Stack + project lifecycle & config

| ✅/🌐 | Method | Path | operationId |
|---|---|---|---|
| ✅ | HEAD   | `/api/stacks/{orgName}/{projectName}`                       | `ProjectExists` |
| ✅ | POST   | `/api/stacks/{orgName}/{projectName}`                       | `CreateStack` |
| ✅ | GET    | `/api/stacks/{orgName}/{projectName}/{stackName}`           | `GetStack` |
| ✅ | DELETE | `/api/stacks/{orgName}/{projectName}/{stackName}`           | `DeleteStack` |
| ✅ | GET    | `/api/stacks/{orgName}/{projectName}/{stackName}/config`    | `GetStackConfig` |
| ✅ | PUT    | `/api/stacks/{orgName}/{projectName}/{stackName}/config`    | `UpdateStackConfig` |
| ✅ | DELETE | `/api/stacks/{orgName}/{projectName}/{stackName}/config`    | `DeleteStackConfig` |
| ✅ | GET    | `/api/stacks/{orgName}/{projectName}/{stackName}/updates/latest` | `GetLatestStackUpdate` (CLI: `GetLatestConfiguration`) |

### 1b. State export/import & secrets (encryption)

| ✅/🌐 | Method | Path | operationId |
|---|---|---|---|
| ✅ | GET  | `/api/stacks/{orgName}/{projectName}/{stackName}/export`        | `ExportStack` |
| ✅ | POST | `/api/stacks/{orgName}/{projectName}/{stackName}/import`        | `ImportStack` |
| ✅ | POST | `/api/stacks/{orgName}/{projectName}/{stackName}/encrypt`       | `EncryptValue` |
| ✅ | POST | `/api/stacks/{orgName}/{projectName}/{stackName}/decrypt`       | `DecryptValue` |
| ✅ | POST | `/api/stacks/{orgName}/{projectName}/{stackName}/batch-encrypt` | `BatchEncryptValue` |
| ✅ | POST | `/api/stacks/{orgName}/{projectName}/{stackName}/batch-decrypt` | `BatchDecryptValue` |

### 1c. The update lifecycle — `update` kind (`pulumi up`)

The CLI drives every deployment through this exact sequence. **Get this one working end-to-end first**, then
clone it for the other three kinds (they are byte-for-byte identical except the `update` path segment).

Order of calls during a real `pulumi up`:

1. `CreateUpdateForUpdate` — create the update record
2. `StartUpdateForUpdate` — start it; returns a token + lease
3. `RenewUpdateLease_update` — keep the lease alive during long runs
4. `RecordEngineEvent_update` / `RecordEngineEventBatch_update` — stream engine events up
5. `PatchUpdateCheckpoint_update` (+ `…Delta` / `…Verbatim`) — persist the state checkpoint
6. `CreateJournalEntries_update` — journal-based state (newer; gated by `Capabilities`)
7. `AppendUpdateLogEntry_update` — human-readable logs
8. `CompleteUpdate_update` — finalize with succeeded/failed status
9. `CancelUpdate_update` — on Ctrl-C
10. `GetUpdateStatusForUpdate` / `GetEngineEvents_update` — read-side (CLI tail/console)

| ✅/🌐 | Method | Path | operationId |
|---|---|---|---|
| ✅ | POST  | `…/{stackName}/update`                           | `CreateUpdateForUpdate` |
| ✅ | POST  | `…/{stackName}/update/{updateID}`                | `StartUpdateForUpdate` |
| ✅ | GET   | `…/{stackName}/update/{updateID}`                | `GetUpdateStatusForUpdate` |
| ✅ | POST  | `…/{stackName}/update/{updateID}/renew_lease`    | `RenewUpdateLease_update` |
| ✅ | POST  | `…/{stackName}/update/{updateID}/events`         | `RecordEngineEvent_update` |
| ✅ | POST  | `…/{stackName}/update/{updateID}/events/batch`   | `RecordEngineEventBatch_update` |
| ✅ | GET   | `…/{stackName}/update/{updateID}/events`         | `GetEngineEvents_update` |
| ✅ | PATCH | `…/{stackName}/update/{updateID}/checkpoint`         | `PatchUpdateCheckpoint_update` |
| ✅ | PATCH | `…/{stackName}/update/{updateID}/checkpointdelta`    | `PatchUpdateCheckpointDelta_update` |
| ✅ | PATCH | `…/{stackName}/update/{updateID}/checkpointverbatim` | `PatchUpdateVerbatimCheckpoint_update` |
| ✅ | PATCH | `…/{stackName}/update/{updateID}/journalentries`     | `CreateJournalEntries_update` |
| ✅ | POST  | `…/{stackName}/update/{updateID}/log`            | `AppendUpdateLogEntry_update` |
| ✅ | POST  | `…/{stackName}/update/{updateID}/complete`       | `CompleteUpdate_update` |
| ✅ | POST  | `…/{stackName}/update/{updateID}/cancel`         | `CancelUpdate_update` |

### 1d. Same lifecycle for `preview`, `refresh`, `destroy`

Identical contracts; just the path verb changes. Implement `preview` next (`pulumi preview`), then `destroy`
(`pulumi destroy`), then `refresh` (`pulumi refresh`). Note: `preview` has **no** checkpoint endpoints (it's a
dry run) — it only has create/start/status/events/log/complete/cancel/renew_lease.

- **preview:** `CreateUpdateForPreview`, `StartUpdateForPreview`, `GetUpdateStatusForPreview`,
  `RecordEngineEvent_preview`, `RecordEngineEventBatch_preview`, `GetEngineEvents_preview`,
  `CreateJournalEntries_preview`, `AppendUpdateLogEntry_preview`, `RenewUpdateLease_preview`,
  `CompleteUpdate_preview`, `CancelUpdate_preview`
- **destroy:** `CreateUpdateForDestroy`, `StartUpdateForDestroy`, `GetUpdateStatusForDestroy`,
  `RecordEngineEvent_destroy`, `RecordEngineEventBatch_destroy`, `GetEngineEvents_destroy`,
  `PatchUpdateCheckpoint_destroy`, `PatchUpdateCheckpointDelta_destroy`, `PatchUpdateVerbatimCheckpoint_destroy`,
  `CreateJournalEntries_destroy`, `AppendUpdateLogEntry_destroy`, `RenewUpdateLease_destroy`,
  `CompleteUpdate_destroy`, `CancelUpdate_destroy`
- **refresh:** `CreateUpdateForRefresh`, `StartUpdateForRefresh`, `GetUpdateStatusForRefresh`,
  `RecordEngineEvent_refresh`, `RecordEngineEventBatch_refresh`, `GetEngineEvents_refresh`,
  `PatchUpdateCheckpoint_refresh`, `PatchUpdateCheckpointDelta_refresh`, `PatchUpdateVerbatimCheckpoint_refresh`,
  `CreateJournalEntries_refresh`, `AppendUpdateLogEntry_refresh`, `RenewUpdateLease_refresh`,
  `CompleteUpdate_refresh`, `CancelUpdate_refresh`

> ✅ **Milestone:** after Tiers 0–1, a stock `pulumi` CLI can log in, set config, and run
> `up / preview / refresh / destroy` against HappyPumi with full state + secrets. This is "basic IaC working."

---

## Tier 2 — Stack History, Listing & Tags (everyday UX + minimal IDP visibility)

| ✅/🌐 | Method | Path | operationId |
|---|---|---|---|
| ✅ | GET   | `/api/user/stacks`                                          | `ListUserStacks` |
| ✅ | GET   | `/api/stacks/{orgName}/{projectName}/{stackName}/updates`   | `GetStackUpdates` |
| ✅ | GET   | `/api/stacks/{orgName}/{projectName}/{stackName}/updates/{version}` | `GetStackUpdate` |
| ✅ | POST  | `/api/stacks/{orgName}/{projectName}/{stackName}/rename`    | `RenameStack` |
| ✅ | POST   | `/api/stacks/{orgName}/{projectName}/{stackName}/tags`      | `AddStackTag` |
| ✅ | PATCH  | `/api/stacks/{orgName}/{projectName}/{stackName}/tags`      | `UpdateStackTags` |
| ✅ | DELETE | `/api/stacks/{orgName}/{projectName}/{stackName}/tags/{tagName}` | `DeleteStackTag` (Automation API `RemoveTag` / `pulumi stack tag rm`) |

---

## Tier 3 — IDP Core: Orgs, Members & RBAC

This is where "IDP" begins: who can do what, in which org. All CLI-reachable.

| ✅/🌐 | Method | Path | operationId |
|---|---|---|---|
| ✅ | GET    | `/api/orgs/{orgName}/members`                     | `ListOrganizationMembers` |
| ✅ | POST   | `/api/orgs/{orgName}/members/{userLogin}`         | `AddOrganizationMember` |
| ✅ | PATCH  | `/api/orgs/{orgName}/members/{userLogin}`         | `UpdateOrganizationMember` |
| ✅ | DELETE | `/api/orgs/{orgName}/members/{userLogin}`         | `DeleteOrganizationMember` |
| ✅ | GET    | `/api/orgs/{orgName}/roles`                       | `ListRolesByOrgIDAndUXPurpose` |
| ✅ | POST   | `/api/orgs/{orgName}/roles`                       | `CreateRole` |
| ✅ | GET    | `/api/orgs/{orgName}/roles/{roleID}`              | `GetRole` |
| ✅ | PATCH  | `/api/orgs/{orgName}/roles/{roleID}`              | `UpdateRole` |
| ✅ | DELETE | `/api/orgs/{orgName}/roles/{roleID}`              | `DeleteRole` |
| ✅ | POST   | `/api/orgs/{orgName}/teams/{teamName}/roles/{roleID}` | `UpdateTeamRoles` |
| ✅ | DELETE | `/api/orgs/{orgName}/teams/{teamName}/roles/{roleID}` | `DeleteTeamRole` |
| ✅ | GET    | `/api/orgs/{orgName}/auditlogs`                   | `ListAuditLogEventsHandlerV1` |

---

## Tier 4 — IDP Self-Service: Templates & Registry

Powers `pulumi new`, org templates, and package/provider distribution — the self-service catalog of an IDP.

| ✅/🌐 | Method | Path | operationId |
|---|---|---|---|
| ✅ | GET    | `/api/orgs/{orgName}/templates`                   | `GetOrgTemplates` |
| ✅ | GET    | `/api/orgs/{orgName}/template/download`           | `GetOrgTemplateDownload` |
| ✅ | POST   | `/api/ai/template`                                | `AITemplate` |
| ✅ | GET    | `/api/registry/packages`                          | `ListPackages` |
| ✅ | GET    | `/api/registry/packages/{source}/{publisher}/{name}/versions/{version}` | `GetPackageVersion` |
| ✅ | POST   | `/api/registry/packages/{source}/{publisher}/{name}/versions`           | `PostPublishPackageVersion` |
| ✅ | POST   | `/api/registry/packages/{source}/{publisher}/{name}/versions/{version}/complete` | `PostPublishPackageVersionComplete` |
| ✅ | DELETE | `/api/registry/packages/{source}/{publisher}/{name}/versions/{version}` | `DeletePublishPackageVersion` |
| ✅ | GET    | `/api/registry/templates`                         | `ListTemplates` |
| ✅ | GET    | `/api/registry/templates/{source}/{publisher}/{name}/versions` | `ListTemplateVersions` |
| ✅ | GET    | `/api/registry/templates/{source}/{publisher}/{name}/versions/{version}` | `GetTemplateVersion` |
| ✅ | POST   | `/api/registry/templates/{source}/{publisher}/{name}/versions`           | `PostPublishTemplateVersion` |
| ✅ | POST   | `/api/registry/templates/{source}/{publisher}/{name}/versions/{version}/complete` | `PostPublishTemplateVersionComplete` |
| ✅ | DELETE | `/api/registry/templates/{source}/{publisher}/{name}/versions/{version}` | `DeleteTemplateVersion` |

---

## Tier 5 — IDP Guardrails: Policy-as-Code (CrossGuard)

Org-wide policy packs + results — the compliance backbone of an IDP. All CLI-reachable.

| ✅/🌐 | Method | Path | operationId |
|---|---|---|---|
| ✅ | GET    | `/api/orgs/{orgName}/policygroups`                | `ListPolicyGroups` |
| ✅ | POST   | `/api/orgs/{orgName}/policygroups`                | `NewPolicyGroup` |
| ✅ | GET    | `/api/orgs/{orgName}/policygroups/{policyGroup}`  | `GetPolicyGroup` |
| ✅ | PATCH  | `/api/orgs/{orgName}/policygroups/{policyGroup}`  | `UpdatePolicyGroup` |
| ✅ | DELETE | `/api/orgs/{orgName}/policygroups/{policyGroup}`  | `DeletePolicyGroup` |
| ✅ | GET    | `/api/orgs/{orgName}/policypacks`                 | `ListPolicyPacks_orgs` |
| ✅ | POST   | `/api/orgs/{orgName}/policypacks`                 | `CreatePolicyPack` |
| ✅ | GET    | `/api/orgs/{orgName}/policypacks/{policyPackName}/versions/{version}`        | `GetPolicyPack` |
| ✅ | GET    | `/api/orgs/{orgName}/policypacks/{policyPackName}/versions/{version}/schema` | `GetPolicyPackConfigSchema` |
| ✅ | POST   | `/api/orgs/{orgName}/policypacks/{policyPackName}/versions/{version}/complete` | `CompletePolicyPack` |
| ✅ | DELETE | `/api/orgs/{orgName}/policypacks/{policyPackName}/versions/{version}`        | `DeletePolicyPackVersion` |
| ✅ | DELETE | `/api/orgs/{orgName}/policypacks/{policyPackName}` | `DeletePolicyPack_orgs_policypacks` |
| ✅ | GET    | `/api/stacks/{orgName}/{projectName}/{stackName}/policypacks` | `GetStackPolicyPacks` |
| ✅ | POST   | `/api/orgs/{orgName}/policyresults/compliance`    | `GetPolicyComplianceResults` |
| ✅ | POST   | `/api/orgs/{orgName}/policyresults/issues`        | `ListPolicyIssues` |
| ✅ | GET    | `/api/orgs/{orgName}/policyresults/issues/{issueId}` | `GetPolicyIssue` |
| ✅ | PATCH  | `/api/orgs/{orgName}/policyresults/issues/{issueId}` | `UpdatePolicyIssue` |

---

## Tier 6 — Managed Deployments (Pulumi Deployments / GitOps IDP)

Deploy-from-Git, deployment settings, schedules, drift detection, webhooks. Optional for basic IaC, but a
major IDP feature (self-service deploys without local CLI). All CLI-reachable.

| ✅/🌐 | Method | Path | operationId |
|---|---|---|---|
| ✅ | GET    | `…/{stackName}/deployments`                      | `ListStackDeploymentsHandlerV2` |
| ✅ | POST   | `…/{stackName}/deployments`                      | `CreateAPIDeploymentHandlerV2` |
| ✅ | POST   | `…/{stackName}/deployments/{deploymentId}/cancel`| `CancelDeployment` |
| ✅ | GET    | `…/{stackName}/deployments/settings`             | `GetDeploymentSettings` |
| ✅ | POST   | `…/{stackName}/deployments/settings`             | `PatchDeploymentSettings` |
| ✅ | PUT    | `…/{stackName}/deployments/settings`             | `ReplaceDeploymentSettings` |
| ✅ | DELETE | `…/{stackName}/deployments/settings`             | `DeleteDeploymentSettings` |
| ✅ | POST   | `…/{stackName}/deployments/settings/encrypt`     | `EncryptDeploymentSettingsSecret` |
| ✅ | GET    | `…/{stackName}/deployments/schedules`            | `ListScheduledDeployment` |
| ✅ | POST   | `…/{stackName}/deployments/schedules`            | `CreateScheduledDeployment` |
| ✅ | POST   | `…/{stackName}/deployments/drift/schedules`      | `CreateScheduledDriftDeployment` |
| ✅ | POST   | `…/{stackName}/deployments/ttl/schedules`        | `CreateScheduledTTLDeployment` |
| ✅ | GET    | `…/{stackName}/drift/runs`                       | `ListDriftRuns` |
| ✅ | GET    | `…/{stackName}/drift/status`                     | `GetStackDriftStatus` |
| ✅ | GET    | `…/{stackName}/hooks`                            | `ListStackWebhooks` |
| ✅ | POST   | `…/{stackName}/hooks`                            | `CreateStackWebhook` |

---

## Tier 7 — Observability & Search (nice-to-have IDP dashboards)

| ✅/🌐 | Method | Path | operationId |
|---|---|---|---|
| ✅ | GET | `/api/orgs/{orgName}/search/resources`        | `GetOrgResourceSearchQuery` |
| ✅ | GET | `/api/orgs/{orgName}/search/resources/parse`  | `GetNaturalLanguageQuery` |
| ✅ | GET | `/api/orgs/{orgName}/resources/summary`       | `GetUsageSummaryResourceHours` |

---

## Tier 8 — Defer / Out of Scope for "basic" (large separate subsystems)

These are real Pulumi Cloud features but the CLI either never calls them for core IaC, or they are
**self-contained products** with their own clients. Skip until the tiers above are solid.

| Subsystem (spec tag) | ~Ops | Why deferred |
|---|---|---|
| **Environments / ESC** | ~120 | Pulumi ESC is a separate product with its own `esc` CLI. The `pulumi` CLI only calls `GET /api/esc/environments` (`ListEnvironments_esc`) for stack-config `environments:` references. Huge surface; implement only that one endpoint if you need ESC-backed config, defer the rest. |
| **Insights / InsightsAccounts** | ~46 | Cloud asset scanning (`ListAccounts`, `ScanAccount`, `GetScan`, …). Reachable via `pulumi insights` but not needed for IaC. |
| **VCS Integrations** | ~43 | GitHub/GitLab app wiring. Only `GetGHAppIntegration` (`/api/console/orgs/{orgName}/integrations/github-app`) is CLI-touched. Mostly console-driven (🌐). |
| **AI / AI Agents / Neo** | ~9 | Copilot, `pulumi ai`, Neo agent tasks (`CreateTasks`, `StreamTaskEvents`, …). Pure value-add. |
| **Workflows** | ~5 | Org workflow automation; console-driven. |
| **CloudSetup, DataExport, OAuthTokenExchange (beyond /token), OidcIssuers, Schedules, Services, ResourcesUnderManagement, AccessTokens** | rest | Console / admin / billing surfaces. ~427 total operations across the spec are **never called by the CLI** — treat them as console-only (🌐) and implement on demand. |

---

## Suggested execution order (TL;DR)

1. **Tier 0** — login/identity (6 endpoints) → CLI can authenticate.
2. **Tier 1c (`update` kind)** + **1a/1b** → first successful `pulumi up` with state & secrets.
3. **Tier 1d** — clone lifecycle to `preview` / `destroy` / `refresh` → full IaC loop. **← "basic IaC done"**
4. **Tier 2** — history, listing, tags → usable day-to-day.
5. **Tier 3** — orgs/members/RBAC → **"basic IDP done"**.
6. **Tiers 4–6** — templates/registry, policy, deployments → full IDP platform.
7. **Tiers 7–8** — search, dashboards, then the large optional subsystems as needed.
