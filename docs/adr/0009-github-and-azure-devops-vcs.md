# ADR-0009 — Support both GitHub and Azure DevOps as VCS providers

**Status:** Accepted
**Date:** 2026-06-18

## Context

HappyPumi is more than a state backend — it is an IDP, and the IDP features depend on a version-control
system (VCS): the spec exposes VCS integrations (`VcsIntegrations` area), and source-control linkage shows up
across the product — review stacks / deployments triggered from commits and pull requests, templates and the
registry sourced from repositories, and the user identity model itself (`User.githubLogin`, and the
`/api/user` contract the CLI validates login against — see Tier 0 in [ENDPOINTS.md](../../ENDPOINTS.md)).

The upstream Pulumi Cloud spec is heavily GitHub-shaped (the canonical user handle is literally
`githubLogin`). But our target deployments are not GitHub-only: a significant share of self-hosters live in
**Azure DevOps** (repos, pipelines, and AAD-backed identity). If we let GitHub assumptions leak into the core
— hard-coded webhook payload shapes, `githubLogin` treated as *the* identity, GitHub-only OAuth — we would
have to retrofit a second provider later at much higher cost.

We need to decide this now because it constrains the VCS abstraction, the identity/claims mapping
([ADR-0007](0007-openid-authentication-rbac.md)), and the data model
([ADR-0005](0005-postgresql-database.md)) — all of which are being built out as endpoints land.

## Decision

HappyPumi **must support both GitHub and Azure DevOps** as first-class VCS providers, and the architecture
must not privilege one over the other.

- **Provider abstraction:** VCS access (repo listing, webhooks, commit/PR status, clone/auth) is defined
  behind a single `IVcsProvider`-style interface owned by HappyPumi (per CLAUDE.md: wrap third-party libs
  behind a thin interface). `GitHubVcsProvider` and `AzureDevOpsVcsProvider` are two implementations selected
  by configuration; adding a third provider later means adding an implementation, not editing the core.
- **Provider-neutral domain model:** the persisted model uses neutral concepts (provider id, repository,
  branch, commit, pull/merge request, webhook) rather than GitHub-specific fields. Where the wire contract
  forces a GitHub-named field (e.g. `githubLogin`), we keep wire-compatibility on the contract but back it
  with a provider-neutral identity internally, mapping the active provider's handle onto that field.
- **Identity:** OIDC ([ADR-0007](0007-openid-authentication-rbac.md)) is the auth mechanism; both GitHub and
  Azure DevOps (AAD/Entra) federate through it. The claims→identity mapping must populate the same internal
  user regardless of which provider issued the token.
- **Webhooks & events:** inbound webhook handling normalizes each provider's payload to a common internal
  event shape before any business logic runs; provider-specific parsing lives only in the provider
  implementation.
- **Both providers are tested:** any VCS-touching feature must have coverage for GitHub *and* Azure DevOps
  (contract tests against the `IVcsProvider` interface plus per-provider tests with fakes for the external
  API — see [ADR-0003](0003-xunit-testing.md)). A feature is not "done" if it only works for GitHub.

## Consequences

- **Positive:** Azure DevOps shops are first-class, not an afterthought; the provider seam keeps GitHub
  assumptions out of the core and leaves room for GitLab/Bitbucket later.
- **Positive:** Forces a clean separation between the (GitHub-shaped) public wire contract and an internal
  provider-neutral domain model, which is healthy regardless of VCS count.
- **Trade-off:** More upfront abstraction and double the integration surface to build and test — every
  VCS feature ships two implementations and two test suites.
- **Trade-off:** The `githubLogin`-style fields in the public spec remain a leaky GitHub-ism we must carry
  for wire-compatibility; we map around it rather than rename it, and document that mapping where it matters.
- **Follow-up:** Define the `IVcsProvider` interface and the normalized webhook/event shape; specify the
  Azure DevOps OAuth/PAT and AAD claims mapping; decide how provider selection is configured (per-org vs.
  global) when the VCS endpoints in the `VcsIntegrations` area are implemented.
