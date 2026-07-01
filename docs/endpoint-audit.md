# HappyPumi endpoint audit — implementation status

> Regenerated 2026-07-01 from the live codebase (`NotImplementedException` stubs). **148 endpoints remain unimplemented** (was 355 at the first audit, 318 after Stack Detail, 292 after VCS, 224 after settings+webhooks, 207 after change-requests, 191 after templates+policy). What remains is entirely the large self-contained products (Insights, ESC preview/v2, Registry preview) plus Deployments extras and the intentionally-out-of-scope VCS providers; the CLI + Automation API IaC core and the whole org-admin surface are fully implemented.

## Implemented since the first audit

- **Org-admin long-tail — COMPLETE** (PRs #67–#72, 43 endpoints): org core/settings/members/roles
  (`IOrgSettingsStore` + `IIdentityStore` extensions); audit-log query & export over `IAuditLog` +
  `IAuditExportConfigStore`; services catalog (`IServiceStore` items) + the remaining agent-pool
  delete/patch; resource search/dashboard + usage summaries (deterministic empties/zeros where no
  resource store, env-count from `IEnvironmentStore`); stack restore & bulk transfer
  (`IDeletedStackStore` tombstones hooked into `DeleteStack` + `IStackStore.Transfer`); and the
  `/api/user/*` account surface (`IUserAccountStore` + `CurrentUserFactory`).
- **Templates + Policy results — COMPLETE** (PRs #64–#65, 16 endpoints): org template **sources** CRUD
  (`ITemplateSourceStore`) + project-template resolution over the existing registry; CrossGuard
  **policy results** (`PolicyResultsAggregator` computes metadata / issue filters / compliance /
  CSV export over the existing `IPolicyFindingStore`) + **policy groups** (metadata, batch pack
  assignment, per-stack groups) over `IPolicyStore` + `GetOrgRegistryPolicyPack`; plus `UpdateAuthPolicy`
  (new `IAuthPolicyStore`, `GetAuthPolicy` rewired to read it). Console templates + policy-results pages.
- **Change requests, gates & approvals — COMPLETE** (PRs #60–#62, 17 endpoints): a full-real PR-like
  workflow for ESC environments — a change request wraps an env draft, moves through a status
  lifecycle with an event timeline, and `apply` commits the draft as a new revision once **change
  gates** (approval-required rules, `IChangeGateStore`) are satisfied (`ChangeGateEvaluator` blocks
  apply below the approval threshold; honors self-approval / `requireReapprovalOnChange`).
  `Approve`/`Unapprove` reclaimed via dispatch-by-ID (CR first, else the existing ESC secret-reveal
  grant path). Includes the 4 `/api/preview/esc/.../drafts` endpoints; console CR review + gates pages.
- **Settings cluster — COMPLETE** (PRs #53–#58, 46 endpoints): Access tokens (issue-once/list/revoke for personal/org/team), Teams & permissions (update/delete/roles/enable/teams-with-role), OIDC issuers (register/CRUD + real thumbprint fetch + auth policy), Encryption keys / BYOK (CMK records + KEK migrations + project value encrypt/decrypt via `IValueCrypter`), **SAML/SSO full-real** (config + admins + a real SP-side ACS endpoint doing XML-DSig assertion verification via `ISamlAssertionValidator`), and **Cloud accounts full-real OAuth** (`ICloudSetupProvider` AWS/Azure/GCP seam, config-gated real authorization URLs / token exchange / account listing). Console settings pages for each.
- **Webhooks — COMPLETE for org + stack + ESC environment** (PRs #50–#52, 22 endpoints): shared event-fired `WebhookDispatcher` (HMAC signing, per-format formatters, SSRF deny-list, fail-fast delivery), delivery history, ping/redeliver, firing wired into update-complete / deployment-status / env-mutation points; console webhook management at all three scopes.
- **VCS integrations — COMPLETE for GitHub + GitHub Enterprise + Azure DevOps** (PRs #46–#48, ~26 endpoints): integration-record CRUD, the `IVcsProvider` seam (ADR-0009) with real-REST GitHub + Azure DevOps providers, GitHub App setup / ADO OAuth, org-teams, and repo/branch listing; console connect flow + repo browser. **GitLab, BitBucket, and custom VCS are intentionally out of scope** (their ~20 endpoints remain stubs).
- **Stack Detail — COMPLETE** (PRs #38–#43, ~26 endpoints): resource/update/preview detail, engine-event persistence, activity, access, references, settings actions.
- **`DeleteStackTag`** (PR #36); **AI Agents / Neo removed** (PR #37, excluded from the generator).

## Remaining unimplemented, by console page / feature

Confidence: **A** = under `/api/console/*` (console-only API). Sorted by size.

| Feature / console page | Remaining | of which `/api/console/*` |
|---|---:|---:|
| Insights (cloud scanning) | 46 | 0 |
| ESC environments (preview/v2 extras) | 43 | 0 |
| Registry (preview + packages) | 21 | 0 |
| VCS integrations (GitLab/BitBucket/custom — out of scope) | 19 | 19 |
| Deployments (schedules/controls/usage) | 19 | 0 |

**Total remaining: 148.**

## Suggested next feature group

All the small/cohesive surfaces are now done (settings, webhooks, first-class VCS, change
requests/gates, templates + policy results, and the entire org-admin long-tail). What remains is
**only the large self-contained products** plus the Deployments extras and the intentionally-excluded
VCS providers — each its own subsystem warranting a dedicated brainstorm before implementation:

- **Insights (cloud scanning)** — 46 endpoints (`/api/preview/insights/*`): account onboarding,
  resource discovery/scanning, insight results. Its own product with its own data model.
- **ESC preview/v2 extras** — 43 endpoints (`/api/preview/esc/*` + v2 duplicates): the preview-route
  variants and extras beyond the core ESC surface already implemented.
- **Registry preview** — 21 endpoints (`/api/preview/registry/*` + packages): the CrossGuard
  policypack + template registry-preview publishing surface.
- **Deployments extras** — 19 endpoints: schedules, controls, usage beyond the core deploy lifecycle.
- **VCS GitLab/BitBucket/custom** — 19 endpoints, intentionally out of scope (ADR-0009 covers GitHub +
  Azure DevOps only).

> This table is the maintained summary; the full per-endpoint listing can be regenerated from the stubs at any time.
