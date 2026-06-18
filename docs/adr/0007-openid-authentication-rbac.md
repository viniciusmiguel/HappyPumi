# ADR-0007 — OpenID Connect for authentication, with RBAC authorization

**Status:** Accepted
**Date:** 2026-06-18

## Context

HappyPumi must authenticate two distinct callers against the same API:

1. **The Pulumi CLI**, which logs in with a bearer **access token** and validates it via `GET /api/user`
   (`GetPulumiAccountDetails`), resolves its org via `GET /api/user/organizations/default`, and — for CI —
   exchanges an OIDC token at `POST /api/oauth/token` (`Token`). See Tier 0 in [ENDPOINTS.md](../../ENDPOINTS.md).
2. **Human users / the console**, who sign in interactively.

Authorization is multi-tenant and role-based: the spec already exposes org members and a roles surface
(`/api/orgs/{orgName}/roles`, team role assignment) — see Tier 3 of ENDPOINTS.md. We need standards-based
identity that supports interactive login, machine tokens, and OIDC token exchange for CI federation, without
HappyPumi becoming a bespoke identity provider.

## Decision

Use **OpenID Connect (OIDC)** for authentication and an **RBAC** model for authorization.

- **Authentication:** delegate identity to an OIDC provider (any standards-compliant IdP — Keycloak, Entra,
  Auth0, etc.; self-hosters pick their own). The API validates **JWT bearer tokens** via the standard
  ASP.NET Core JWT/OIDC authentication handlers.
- **CLI tokens & OIDC exchange:** implement `POST /api/oauth/token` as an OIDC token-exchange endpoint so CI
  systems can federate their workload identity into a HappyPumi access token, matching the CLI's expectations.
- **Authorization (RBAC):** roles and memberships are persisted in PostgreSQL ([ADR-0005](0005-postgresql-database.md))
  and scoped per organization (org/team/stack). Endpoints enforce access via FastEndpoints policies
  (`Roles(...)` / `Policies(...)`), replacing the generated `AllowAnonymous()` stubs.
- Roles map to the spec's roles/members endpoints so the CLI and console manage access through the existing API.

## Consequences

- **Positive:** Standards-based; no custom credential storage or password handling. Works uniformly for
  interactive users, long-lived CLI tokens, and CI via OIDC token exchange. Pluggable IdP keeps the
  self-hosting story open.
- **Positive:** Per-org RBAC backed by the database aligns directly with the Pulumi multi-tenant model and the
  members/roles endpoints already in the spec.
- **Trade-off:** Requires an OIDC provider to be available/configured — added setup for self-hosters; we
  must document a default for local dev (resolved below).
- **Follow-up:** Define the token/claims-to-RBAC mapping, the permission taxonomy (what each role may do per
  resource), and the personal-access-token story for the CLI. Every generated endpoint's `AllowAnonymous()`
  placeholder must be replaced with an explicit policy before it is considered implemented.

## Local development default

For local dev/testing the bundled OIDC provider is **Dex** (lightweight, open source), run as a container
in the Aspire AppHost ([ADR-0003](0003-xunit-testing.md)) with static users/groups seeded in
`HappyPumi.AppHost/dex/config.yaml` — `groups` map to RBAC roles. The API talks **HTTPS using a self-signed
certificate** (the ASP.NET Core dev cert): clients trust it via OpenSSL's `SSL_CERT_DIR` (the .NET test
hosts and the Go pulumi CLI both honour it). `make certs` creates and trusts the cert. Self-hosters remain
free to point the API at any standards-compliant IdP and their own TLS certificate.
