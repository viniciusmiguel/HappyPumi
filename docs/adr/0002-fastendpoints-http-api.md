# ADR-0002 — FastEndpoints (REPR) for the HTTP API

**Status:** Accepted
**Date:** 2026-06-18

## Context

The API surface is large (~588 operations across 14 areas — see [ENDPOINTS.md](../../ENDPOINTS.md)) and is
**generated** from `pulumi-spec.json`. We need an HTTP framework where each operation is an isolated,
strongly-typed unit that maps cleanly 1:1 to a spec operation, is trivial to code-generate, and is testable in
isolation. We also need OpenAPI/Swagger output so we can diff our surface against the real Pulumi spec.

The main alternatives were ASP.NET Core MVC controllers (heavyweight, many ops per controller — awkward to
generate and review) and Minimal APIs (terse but weaker request-validation/model-binding ergonomics and no
natural per-operation class to generate into).

## Decision

Use **FastEndpoints** (`FastEndpoints` + `FastEndpoints.Swagger`, v8.x) following the
**REPR (Request-Endpoint-Response)** pattern.

- Each operation is one `Endpoint<TRequest, TResponse>` class in the area folder
  (e.g. `Stacks/CreateUpdateForUpdateEndpoint.cs`), named after the spec `operationId`.
- Request/response DTOs live in `Contracts/` and are likewise generated from the spec's schemas.
- Route, verb, tags, summary, and name come straight from the spec via the generator; the developer fills in
  only `HandleAsync`.
- Swagger is exposed via `SwaggerDocument()` / `UseSwaggerGen()` so our generated surface can be compared
  against `pulumi-spec.json`.

## Consequences

- **Positive:** Clean 1:1 mapping operationId → endpoint class makes codegen and reverse-engineering reviews
  straightforward; each endpoint is independently testable.
- **Positive:** Built-in model binding, validation, and Swagger reduce boilerplate versus MVC.
- **Trade-off:** A third-party dependency (not in the BCL); contributors must learn FastEndpoints conventions
  rather than the more widely known MVC controller model.
- **Operational:** Generated endpoints ship as stubs that `throw new NotImplementedException`; "implemented"
  is defined as a real `HandleAsync` body. The generator must not overwrite hand-written handler bodies.
