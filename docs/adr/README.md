# Architecture Decision Records

This directory records the significant architectural decisions for HappyPumi — an open-source
reimplementation of the Pulumi Cloud API.

We use lightweight [MADR](https://adr.github.io/madr/)-style records: one decision per file, numbered
sequentially, never deleted. When a decision changes, add a new ADR that supersedes the old one and update
the old one's status — don't rewrite history.

## Format

Each record has: **Status**, **Context**, **Decision**, **Consequences**. Status is one of
`Proposed` · `Accepted` · `Superseded by ADR-XXXX` · `Deprecated`.

## Index

| ADR | Title | Status |
|---|---|---|
| [0001](0001-dotnet-10-runtime.md) | .NET 10 / C# as the runtime and language | Accepted |
| [0002](0002-fastendpoints-http-api.md) | FastEndpoints (REPR) for the HTTP API | Accepted |
| [0003](0003-xunit-testing.md) | xUnit + Aspire.Hosting.Testing for tests | Accepted |
| [0004](0004-docker-container-packaging.md) | Ship as a Docker container; Aspire for local orchestration | Accepted |
| [0005](0005-postgresql-database.md) | PostgreSQL as the database | Accepted |
| [0006](0006-opentelemetry-observability.md) | OpenTelemetry for telemetry and logging | Accepted |
| [0007](0007-openid-authentication-rbac.md) | OpenID Connect for authentication, with RBAC | Accepted |
| [0008](0008-clean-room-implementation.md) | Clean-room implementation from public sources only | Accepted |
| [0009](0009-github-and-azure-devops-vcs.md) | Support both GitHub and Azure DevOps as VCS providers | Accepted |
| [0010](0010-audit-logging-infrastructure-actions.md) | Audit every infrastructure-changing action to pluggable blob storage | Accepted |

## Creating a new ADR

Copy the structure of an existing record, take the next number, and add a row to the index above.
