# ADR-0010 — Audit every infrastructure-changing action to pluggable blob storage

**Status:** Accepted
**Date:** 2026-06-18

## Context

HappyPumi mediates changes to real infrastructure: the update lifecycle (`up` / `refresh` / `destroy`),
stack create/delete, configuration and secret changes, organization/member/RBAC changes
([ADR-0007](0007-openid-authentication-rbac.md)), and managed deployments. For any multi-tenant control
plane these are exactly the events an operator, a security team, or a compliance auditor must be able to
reconstruct after the fact: *who* did *what*, to *which* resource, *when*, from *where*, and with *what
result*.

This is distinct from the operational telemetry in [ADR-0006](0006-opentelemetry-observability.md).
OpenTelemetry traces/metrics/logs are for debugging and are sampled, mutable, and short-lived. An **audit
log** is a compliance artifact: append-only, complete (not sampled), long-retained, and tamper-evident. The
two must not be conflated.

Audit records also must outlive the application database and be portable across clouds: self-hosters run on
Azure, on AWS, and on-prem, and auditors often require the audit store to be separate from (and less mutable
than) the operational store. Object/blob storage (write-once, lifecycle/retention policies, cheap long-term
retention) is the natural system of record — but we cannot hard-code one vendor's SDK.

## Decision

**Every infrastructure-changing action emits an immutable audit record, persisted to blob storage through a
provider abstraction that supports Azure, AWS, and generic/S3-compatible backends.**

- **What is audited:** any state-mutating control-plane action — update lifecycle start/complete/cancel per
  kind, stack create/delete, config/secret set/remove, org/team/member/role changes, deployment triggers,
  token issuance/revocation. Read-only requests are not audited. Each generated endpoint that mutates infra
  is responsible for emitting an audit event (enforced by review, like the auth-policy rule in CLAUDE.md).
- **Record shape:** structured JSON (per CLAUDE.md logging rules), one record per action, including: actor
  identity and token/subject (from OIDC — [ADR-0007](0007-openid-authentication-rbac.md)), organization,
  action, target resource (org/stack/update ids), timestamp (UTC), source IP / user agent, the outcome
  (success/failure + reason), and a correlation id that ties back to the OpenTelemetry trace
  ([ADR-0006](0006-opentelemetry-observability.md)).
- **Storage abstraction:** writes go through a single HappyPumi-owned `IAuditLogStore`-style interface
  (same thin-wrapper discipline as the VCS seam in [ADR-0009](0009-github-and-azure-devops-vcs.md)).
  Implementations: `AzureBlobAuditLogStore` (Azure Blob Storage), `S3AuditLogStore` (AWS S3), and a generic
  S3-compatible store (e.g. MinIO) for on-prem/other clouds. The backend is chosen by configuration; adding
  a provider means adding an implementation, not editing the core.
- **Durability over availability (fail-closed):** because the requirement is that these actions *must* be
  logged, an infrastructure-changing action that cannot record its audit event is treated as failed (or
  durably queued for guaranteed later flush) rather than silently proceeding. The audit path must not be a
  place where records are best-effort dropped.
- **Append-only / tamper-evident:** records are write-once (object-store immutability / object-lock and
  retention policies where available). A per-record or per-segment hash chain to detect tampering is a
  follow-up, not required for the first cut.
- **Operational queryability:** recent audit events may also be indexed in PostgreSQL
  ([ADR-0005](0005-postgresql-database.md)) for fast in-product querying, but blob storage remains the
  durable system of record for retention.

## Consequences

- **Positive:** A complete, portable, vendor-neutral audit trail of who changed what — satisfies common
  compliance needs and works on Azure, AWS, and on-prem without code changes.
- **Positive:** Clean separation of audit (compliance) from telemetry (debugging) keeps each fit for purpose
  and avoids accidentally sampling away or mutating audit data.
- **Trade-off:** Fail-closed auditing couples action success to audit-store availability; we mitigate with a
  durable local queue/outbox so a transient blob outage degrades to delayed flush rather than outright
  failure, but this adds moving parts.
- **Trade-off:** Three storage implementations (plus the generic one) to build and test — each needs
  contract tests against the `IAuditLogStore` interface, with fakes for the external SDK (per
  [ADR-0003](0003-xunit-testing.md)).
- **Follow-up:** Define the canonical audit record schema and versioning; the outbox/queue design for
  fail-closed delivery; retention/lifecycle defaults; the hash-chain tamper-evidence scheme; and how the
  store is configured per org vs globally.
