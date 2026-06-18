# ADR-0005 — PostgreSQL as the database

**Status:** Accepted
**Date:** 2026-06-18

## Context

HappyPumi is a stateful service. The Pulumi Cloud API it reimplements must durably persist, at minimum:
organizations, users, and RBAC; projects and stacks; stack config and tags; the update lifecycle (update
records, status, leases, engine events, logs); and — most demanding — **stack state checkpoints** including
verbatim and **delta** checkpoint patches and journal entries (see the update lifecycle in
[ENDPOINTS.md](../../ENDPOINTS.md)). Checkpoints are large, deeply nested JSON documents; config and tags are
key/value; audit logs and engine events are append-heavy.

We need a single primary store that handles relational integrity (orgs → stacks → updates), large JSON
documents (checkpoints/exports), and append-heavy event streams, is open-source and self-hostable to match
the project's "run your own Pulumi Cloud" goal, and integrates cleanly with .NET and Aspire
([ADR-0004](0004-docker-container-packaging.md)).

## Decision

Use **PostgreSQL** as the primary database.

- Relational tables model the core domain (orgs, users, roles, projects, stacks, updates).
- **`jsonb`** columns store the large semi-structured payloads (deployment checkpoints, stack exports,
  config objects, engine-event payloads), giving us indexed querying without a separate document store.
- It is provisioned as an Aspire resource in `HappyPumi.AppHost` for local dev and runs as a container/managed
  instance in production.

Choice of .NET data-access layer (e.g. EF Core with Npgsql vs. a lighter mapper) is **deferred to a later
ADR**; this decision fixes only the database engine.

## Consequences

- **Positive:** One engine covers relational + document + append workloads, avoiding premature polyglot
  persistence. `jsonb` is a strong fit for checkpoints and exports. Mature, free, self-hostable, first-class
  Npgsql/EF Core and Aspire support.
- **Positive:** Transactions give us the consistency needed around the update lifecycle (e.g. lease + status +
  checkpoint changes applied atomically).
- **Trade-off:** Very large checkpoints may eventually need external blob storage with Postgres holding only
  metadata/pointers; revisit if checkpoint sizes pressure the database. Delta-checkpoint reconstruction logic
  must be designed carefully on top of the chosen schema.
- **Follow-up:** A subsequent ADR will decide the ORM/data-access approach and migrations tooling; update
  `AppHost.cs` to add the Postgres resource and wire the connection string into `HappyPumi.Api`.
