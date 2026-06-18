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

### Data-access decision (resolved)

The data-access layer is **EF Core + Npgsql**, code-first with `dotnet ef` migrations. The schema is a
**relational + jsonb hybrid** derived from the wire contracts: a table per persisted aggregate
(`Stacks`, `StackUpdates` history, `Updates`, `Members`, `Roles`, `TeamRoles`, `Packages`, `Templates`,
`PolicyGroups`, `PolicyPackVersions`, `DeploymentSettings`, `Deployments`, `Schedules`, `Webhooks`) with
scalar columns for the fields endpoints query/sort on and `jsonb` columns (via a System.Text.Json value
converter — `Data/Jsonb.cs`) for nested contract payloads (configs, checkpoints, permission descriptors,
deployment settings, webhooks, schedules). The persistence seam is the `I*Store` interfaces in
`HappyPumi.Api/State`; the `Postgres*` implementations in `HappyPumi.Api/Data/Stores` replace the original
in-memory ones (which remain as fast unit-test doubles). The migration is applied at startup
(`Database.Migrate()`). Tests run against a throwaway Postgres via **Testcontainers** ([ADR-0003](0003-xunit-testing.md)).

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
