# Settings cluster — design

> Status: approved (brainstorm) · 2026-06-30 · phased across 6 PRs

## Goal

Implement the org/user **settings** admin surface across six areas — access tokens, teams &
permissions, OIDC issuers, encryption keys / BYOK, SAML/SSO, and cloud accounts — taking the two
auth-flow areas (SAML, cloud accounts) **full-real** (real SSO assertion validation, real cloud
OAuth). ~46 currently-`NotImplementedException` endpoints.

## Decisions (from brainstorm)

- All six areas in scope; **SAML and Cloud accounts done full-real** (not config-records-only).
- **6 phased PRs**, lighter/foundational first, the two heavy auth flows last; each merged when CI green.
- Token enforcement stays permissive: this work manages token **records** (issue/list/revoke); it
  does NOT tighten `PulumiTokenAuthHandler` to reject unknown tokens (that would break the existing
  CLI/Automation/test flows — a separate, deliberate change).

## Decomposition (6 PRs)

| PR | Area | Size | Endpoints (operationIds) |
|---|---|---|---|
| 1 | Access tokens | M | `ListPersonalTokens`, `CreatePersonalToken`, `DeletePersonalToken`, `ListOrgTokens`, `CreateOrgToken`, `DeleteOrgToken`, `ListTeamTokens`, `CreateTeamToken`, `DeleteTeamToken`, `ListOrgTokensWithRole` |
| 2 | Teams & permissions | S | `UpdateTeam`, `DeleteTeam`, `ListTeamRoles`, `EnableTeamRoles`, `ListTeamsWithRole` |
| 3 | OIDC issuers | M | `RegisterOidcIssuer`, `ListOrgsOidcIssuers`, `GetOidcIssuer`, `UpdateOidcIssuer`, `DeleteOidcIssuer`, `RegenerateThumbprints`, `GetAuthPolicy` |
| 4 | Encryption keys / BYOK | M | `CreateOrganizationKey`, `ListOrganizationKeys`, `SetDefaultOrganizationKey`, `DisableOrganizationKey`, `DisableAllOrganizationKeys`, `ListOrganizationKeyMigrations`, `RetryOrganizationKeyMigrations`, `EncryptProjectValue`, `DecryptProjectValue`, `BatchDecryptProjectValue` |
| 5 | SAML / SSO | L | `GetSamlOrganization`, `UpdateSamlOrganization`, `ListSamlOrganizationAdmins`, `UpdateSamlOrganizationAdmins` + a real ACS endpoint |
| 6 | Cloud accounts | L | `AwsSetup`, `AwsssoSetup`, `AwsssoInitiate`, `AwsssoListAccounts`, `AzureSetup`, `AzureListAccounts`, `GcpSetup`, `GcpListAccounts`, `InitiateOAuth`, `CompleteOAuth` |

**Out of scope:** GitLab/BitBucket/custom VCS (separate), Insights, ESC preview/v2 duplicates, Registry
preview — and the ESC env encrypt/decrypt endpoints (those belong to the ESC area, not BYOK).

## Per-area architecture

New persistence seams follow ADR-0005 (interface in `State/`, `InMemory*` + `Postgres*` impls, EF
migration, DI in `Program.cs`). HTTP-calling pieces use a typed `HttpClient` with the resilience
handler removed + a short timeout (the webhook fail-fast pattern), faked in tests via `Esc/StubHttpHandler`.

- **Access tokens** — `IAccessTokenStore` `{ Id, Name, Scope (user|org|team), OwnerKey, HashedValue,
  Created, LastUsed }`. Issue: generate a random `pul-<base64>` token, store its SHA-256 hash, return
  the plaintext **once**; list returns metadata only (never the value); delete revokes. `tokens-by-role`
  filters org tokens by role. Enforcement unchanged (see decision above).
- **Teams & permissions** — reuse the existing team store + role store; the 5 endpoints are
  update/delete/role-list/enable wiring.
- **OIDC issuers** — `IOidcIssuerStore` `{ Id, Name, IssuerUrl, Thumbprints, MaxExpiration, Policies }`.
  `RegisterOidcIssuer`/CRUD over records; `RegenerateThumbprints` fetches the issuer's
  OIDC discovery/JWKS over HTTP and derives thumbprints; `GetAuthPolicy` returns an issuer's auth policy.
- **Encryption keys / BYOK** — `ICmkStore` `{ Id, Provider, KeyUri, IsDefault, Enabled, MigrationState }`
  for customer-managed-key registration (create/list/default/disable/disable-all/migration status+retry).
  Project value encrypt/decrypt reuse the existing stack secret machinery (`EncryptValue`/`DecryptValue`)
  scoped to a project.
- **SAML / SSO** — `ISamlConfigStore` `{ Org, IdpMetadataXml, EntityId, SsoUrl, Certificate, Enabled }`
  + admins. A real SP-side ACS POST endpoint validates a signed SAML assertion (XML-DSig verification
  against the configured cert, behind an owned `ISamlAssertionValidator` seam) and establishes a
  session/principal consistent with ADR-0007. `GetSamlOrganization`/`UpdateSamlOrganization` manage config;
  admins list/update. May sub-split (config+admins, then ACS) if the PR grows too large.
- **Cloud accounts** — `ICloudAccountStore` + `ICloudSetupProvider` seam with `Aws`/`Azure`/`Gcp` impls.
  Real OAuth: `InitiateOAuth`/provider-specific initiate build the authorization URL; `CompleteOAuth`
  exchanges the code for credentials and stores a cloud-account record; `*ListAccounts` calls the
  provider to list accounts/subscriptions/projects. Config-gated graceful degradation (unconfigured →
  not-connected / empty, never 500).

## Frontend

Each area gets its console settings surface: an **Access tokens** page (create → show-once dialog, list,
revoke); the **Teams** page gains role/membership management; an **OIDC issuers** page; an **Encryption
keys** page; a **SAML/SSO** settings page; a **Cloud accounts** page (connect/OAuth + accounts list).
Reuse `Card`/`Table`/`Modal`/`Field`/`Badge`; split into `…/settings/<area>` components if files approach
500 lines. New `api.*` fetchers per area.

## Testing & process

- Stores → in-memory unit tests; endpoints → component tests on real Postgres.
- HTTP-calling pieces (OIDC thumbprints, cloud OAuth) → `StubHttpHandler` asserting outbound request
  shape, no network, no-retry clients (webhook fail-fast pattern). SAML → a locally-signed fixture
  assertion validated by the seam.
- Secrets/tokens are write-only on the wire (issue-once or never echoed).
- Console `npm run build && npm run lint` per PR. Coverage > 80% changed C#, duplication < 3%.
- Each PR branches off `main`, granular commits (Co-Authored-By trailer), merged when CI green. No squash.
  Edit only `HandleAsync` bodies in `// <auto-generated />` endpoint files; never contract files. The
  recursive-`Body` generator quirk may still appear on un-touched stubs — use the `Contracts.X` /
  `EndpointWithoutRequest` workaround and report.
- The unrelated pre-existing OIDC/Entra WIP is set aside (stashed + test moved) for the run and restored
  at the end.

## Acceptance (per area)

Every listed endpoint returns real data / performs its action (no `NotImplementedException`), covered by
tests; the matching console page works and builds + lints; SAML validates a signed assertion and cloud
OAuth completes against a stubbed provider in tests. The HTTP-calling clients fail fast (no in-band retries).

## This PR (PR1 — Access tokens)

`IAccessTokenStore` (+ both impls + migration); the 10 token endpoints (issue-once / list / revoke for
personal, org, team + tokens-by-role); console Access-tokens settings page. The design doc lands with this PR.
