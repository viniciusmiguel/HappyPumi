# HappyPumi endpoint audit — implementation status

> Regenerated 2026-07-01 from the live codebase (`NotImplementedException` stubs). **207 endpoints remain unimplemented** (was 355 at the first audit, 318 after Stack Detail, 292 after VCS, 224 after settings+webhooks). These are console / other-product surfaces (ENDPOINTS.md Tier 8); the CLI + Automation API IaC core is fully implemented.

## Implemented since the first audit

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
| ESC environments (preview/v2 extras) | 42 | 0 |
| Insights (cloud scanning) | 46 | 0 |
| Registry (preview + packages) | 23 | 0 |
| VCS integrations (GitLab/BitBucket/custom — out of scope) | 20 | 19 |
| Deployments (schedules/controls/usage) | 19 | 0 |
| Audit log + export | 9 | 0 |
| User account (tokens/email/invites) | 9 | 0 |
| Templates (org collections) | 7 | 0 |
| Policy (CrossGuard results / groups) | 6 | 0 |
| Services catalog | 5 | 0 |
| Resource search / dashboard | 4 | 1 |
| Org settings / members / roles | 4 | 0 |
| Deployments · agent pools | 2 | 0 |
| Misc (restore-stack, secrets, bulk-transfer, auth, …) | 11 | 0 |

**Total remaining: 207.**

## Suggested next feature group

The settings cluster, webhooks, first-class VCS, and change requests/gates are now complete. The
remaining work is the large self-contained subsystems and a handful of smaller org-admin surfaces.
Candidates by value/size:

- **Audit log + export** — 9 endpoints; the store already exists (ADR-0010), this is the query/export surface.
- **Templates (org collections)** — 7 endpoints; pairs with the existing template registry.
- **Policy (CrossGuard results / groups)** — 6 endpoints; pairs with the existing policy findings.
- **Large separate products (explicit go/no-go):** Insights (cloud scanning, 46), ESC preview/v2 duplicates (~42), Registry preview (23). Each is its own subsystem and warrants its own brainstorm.

> This table is the maintained summary; the full per-endpoint listing can be regenerated from the stubs at any time.
