# HappyPumi endpoint audit — implementation status

> Regenerated 2026-06-30 from the live codebase (`NotImplementedException` stubs). **318 endpoints remain unimplemented** (was 355 at the first audit). These are console / other-product surfaces (ENDPOINTS.md Tier 8); the CLI + Automation API IaC core is fully implemented.

## Implemented since the first audit

- **Stack Detail — COMPLETE** (PRs #38–#43): resource detail (by-version + single), update/preview detail + engine-event persistence, activity feed, access/collaborators, stack references, settings actions (~26 endpoints). The Stack Detail console page now has Overview, README, Updates (+ update detail), Activity, References, Deployments, Resources (+ resource detail), Settings, and Access.
- **`DeleteStackTag`** — Automation API `RemoveTag` (PR #36).
- **AI Agents / Neo** — removed entirely (PR #37); excluded from the generator so they no longer count as unimplemented.

## Remaining unimplemented, by console page / feature

Confidence: **A** = under `/api/console/*` (console-only API). Sorted by size.

| Feature / console page | Remaining | of which `/api/console/*` |
|---|---:|---:|
| ESC environments (preview/v2 extras) | 56 | 0 |
| Settings · VCS integrations | 46 | 43 |
| Insights (cloud scanning) | 46 | 0 |
| Registry | 22 | 0 |
| Deployments (schedules/controls/usage) | 19 | 0 |
| Webhooks (org + stack) | 14 | 0 |
| Approvals / change-requests / gates | 13 | 0 |
| Settings · Cloud accounts (ESC setup) | 10 | 0 |
| Settings · Encryption keys / BYOK | 10 | 0 |
| Settings · Access tokens | 10 | 0 |
| Org settings / members / roles | 10 | 0 |
| Audit log + export | 9 | 0 |
| User account (tokens/email/invites) | 8 | 0 |
| Policy (CrossGuard results) | 8 | 0 |
| Settings · OIDC issuers | 7 | 0 |
| Templates (org collections) | 7 | 0 |
| Resource search / dashboard | 6 | 1 |
| Services catalog | 6 | 0 |
| Settings · Teams & permissions | 5 | 0 |
| Settings · SAML/SSO | 4 | 0 |
| Deployments · agent pools | 2 | 0 |

**Total remaining: 318.**

## Suggested next feature group

Per the original audit, **VCS integrations** is the largest pure-console block (GitHub/GitHub-Enterprise/GitLab/BitBucket/Azure DevOps/custom — list/create/get/update/delete + OAuth + repo/branch listing) and an `IVcsProvider` seam already exists (ADR-0009). The large separate subsystems (Insights, ESC preview duplicates, Registry preview) remain explicit go/no-go decisions.

> Full per-endpoint listing (verb + path + operationId) can be regenerated from the stubs at any time; this table is the maintained summary.
