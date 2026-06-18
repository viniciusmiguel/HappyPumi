# ADR-0004 — Ship as a Docker container; Aspire for local orchestration

**Status:** Accepted
**Date:** 2026-06-18

## Context

HappyPumi is a self-hostable backend: the value proposition is "run your own Pulumi Cloud." That means the
primary distribution artifact must be portable, reproducible, and runnable anywhere — not tied to a host's
installed .NET SDK. We also want a low-friction local development experience that can grow to include backing
services (database, object storage, secrets) as the state engine is built out.

## Decision

Package the API as a **Docker container** built with a multi-stage Dockerfile, and use **.NET Aspire**
(`HappyPumi.AppHost`) as the local orchestration / composition root.

- **Image:** multi-stage build — `mcr.microsoft.com/dotnet/sdk:10.0` to restore/build/publish,
  `mcr.microsoft.com/dotnet/aspnet:10.0` as the slim runtime base. Published with `/p:UseAppHost=false` and
  entered via `dotnet HappyPumi.Api.dll`.
- **Runtime:** Linux (`DockerDefaultTargetOS=Linux`), runs as the non-root `$APP_UID`, exposes ports
  **8080** (HTTP) and **8081** (HTTPS).
- **Local dev:** `HappyPumi.AppHost` is the Aspire entry point that models the app and (as the system grows)
  its dependencies, so `dotnet run` on the AppHost brings up the full topology with dashboards/telemetry.

## Consequences

- **Positive:** One reproducible artifact for any host/CI/cloud; runtime image excludes the SDK, keeping it
  small. Non-root by default is a sane security baseline.
- **Positive:** Aspire gives a single place to wire future dependencies (Postgres for state, blob store for
  checkpoints, a secrets backend) and a local dashboard, while the production unit stays a plain container.
- **Trade-off:** Two ways to "run it" (raw container vs. Aspire AppHost) — document which is for production
  (container) vs. local dev (Aspire) to avoid confusion.
- **Follow-up:** As stateful dependencies are introduced, extend `AppHost.cs` (currently a bare builder) and
  add the corresponding resources to both the Aspire model and the production deployment story.
