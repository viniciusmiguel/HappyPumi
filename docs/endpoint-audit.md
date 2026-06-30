# HappyPumi endpoint audit — implementation status

> Regenerated 2026-06-30 from the live codebase (`NotImplementedException` stubs). **292 endpoints remain unimplemented** (was 355 at the first audit, 318 after Stack Detail). These are console / other-product surfaces (ENDPOINTS.md Tier 8); the CLI + Automation API IaC core is fully implemented.

## Implemented since the first audit

- **VCS integrations — COMPLETE for GitHub + GitHub Enterprise + Azure DevOps** (PRs #46–#48, ~26 endpoints): integration-record CRUD, the `IVcsProvider` seam (ADR-0009) with real-REST GitHub + Azure DevOps providers, GitHub App setup / ADO OAuth, org-teams, and repo/branch listing; console connect flow + repo browser. **GitLab, BitBucket, and custom VCS are intentionally out of scope** (their ~23 endpoints remain stubs).
- **Stack Detail — COMPLETE** (PRs #38–#43, ~26 endpoints): resource/update/preview detail, engine-event persistence, activity, access, references, settings actions.
- **`DeleteStackTag`** (PR #36); **AI Agents / Neo removed** (PR #37, excluded from the generator).

## Remaining unimplemented, by console page / feature

Confidence: **A** = under `/api/console/*` (console-only API). Sorted by size.

| Feature / console page | Remaining | of which `/api/console/*` |
|---|---:|---:|
| ESC environments (preview/v2 extras) | 56 | 0 |
| Insights (cloud scanning) | 46 | 0 |
| Registry | 22 | 0 |
| VCS integrations (GitLab/BitBucket/custom — out of scope) | 20 | 19 |
| Deployments (schedules/controls/usage) | 19 | 0 |
| Webhooks (org + stack) | 14 | 0 |
| Approvals / change-requests / gates | 13 | 0 |
| Settings · Access tokens | 10 | 0 |
| Settings · Cloud accounts (ESC setup) | 10 | 0 |
| Settings · Encryption keys / BYOK | 10 | 0 |
| Org settings / members / roles | 10 | 0 |
| Audit log + export | 9 | 0 |
| User account (tokens/email/invites) | 8 | 0 |
| Policy (CrossGuard results) | 8 | 0 |
| Settings · OIDC issuers | 7 | 0 |
| Templates (org collections) | 7 | 0 |
| Services catalog | 6 | 0 |
| Resource search / dashboard | 6 | 1 |
| Settings · Teams & permissions | 5 | 0 |
| Settings · SAML/SSO | 4 | 0 |
| Deployments · agent pools | 2 | 0 |

**Total remaining: 292.**

## Suggested next feature group

The remaining work is the large self-contained subsystems and the org-admin settings pages. Candidates by value/size:

- **Webhooks (org + stack)** — 14 endpoints, self-contained, high console value (notifications/integrations).
- **Settings cluster** — Access tokens, Encryption keys/BYOK, Cloud accounts, SAML/SSO, OIDC issuers, Teams & permissions: many small, cohesive admin pages.
- **Approvals / change-requests / gates** — 13 endpoints, an IDP guardrail surface.
- **Large separate products (explicit go/no-go):** Insights (cloud scanning), ESC preview/v2 duplicates, Registry preview. Each is its own subsystem.

> This table is the maintained summary; the full per-endpoint listing can be regenerated from the stubs at any time.
