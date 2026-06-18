# ADR-0003 — xUnit + Aspire.Hosting.Testing for tests

**Status:** Accepted
**Date:** 2026-06-18

## Context

Correctness of HappyPumi is defined by behavioral compatibility with the Pulumi CLI: the CLI must be able to
log in and run the full update lifecycle against us. That demands integration-level tests that exercise real
endpoints over HTTP, not just isolated unit tests. We also want a single, idiomatic .NET test framework that
plays well with the Aspire app host used for local orchestration ([ADR-0004](0004-docker-container-packaging.md)).

## Decision

Use **xUnit** as the test framework in the `HappyPumi.Api.Tests` project, with
**`Aspire.Hosting.Testing`** for spinning up the application under test.

- Packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector` for
  coverage, and `Aspire.Hosting.Testing` to launch the distributed app in-process.
- Tests target endpoints over HTTP through the Aspire-hosted app, asserting on status codes and contract
  shapes — mirroring how the real CLI client calls the service.
- Coverage is collected via `coverlet.collector`.

## Consequences

- **Positive:** xUnit is the de-facto standard for modern .NET; low friction for contributors. Aspire testing
  gives realistic end-to-end coverage of the wiring, not just handler logic.
- **Positive:** A natural fixture point for "does a real `pulumi` CLI flow work" acceptance tests as endpoints
  graduate from stubs to implementations.
- **Trade-off:** Aspire-hosted integration tests are heavier and slower than pure unit tests; keep a fast unit
  layer for handler logic and reserve hosted tests for cross-cutting flows.
- **Convention:** New endpoints should land with at least a smoke test asserting the route is wired and the
  contract serializes, then behavioral tests as the handler is implemented.
