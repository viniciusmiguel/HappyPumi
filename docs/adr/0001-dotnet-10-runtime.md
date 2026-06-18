# ADR-0001 — .NET 10 / C# as the runtime and language

**Status:** Accepted
**Date:** 2026-06-18

## Context

HappyPumi reimplements the Pulumi Cloud API: a stateful HTTP service that authenticates CLIs, stores stack
state and secrets, and drives the long-running update lifecycle (`up`/`preview`/`refresh`/`destroy`). The
real Pulumi service and CLI are written in Go; we are free to choose our own server stack since the contract
we must honor is the HTTP API in [`pulumi-spec.json`](../../pulumi-spec.json), not Pulumi's source language.

We need a runtime that offers: a fast, async-first HTTP stack; first-class JSON; strong static typing to keep
the ~588-operation surface honest against generated contracts; mature crypto for secret encrypt/decrypt; and
good container/cloud-native support.

## Decision

Use **.NET 10** (`net10.0`) with **C#** across all projects — API, code generator, and tests.

- `Nullable` and `ImplicitUsings` are enabled solution-wide.
- The generator project (`Generator/PulumiApiGenerator`) consumes the OpenAPI spec and emits endpoint and
  contract stubs, so the server stays in lockstep with the spec on the same runtime.

## Consequences

- **Positive:** Single language/toolchain for app, tests, and codegen. Minimal-hosting + async I/O suits the
  streaming engine-event and checkpoint endpoints. Built-in `System.Security.Cryptography` covers the
  secrets endpoints. Strong typing + nullable references catch contract drift at compile time.
- **Positive:** Excellent container story (see [ADR-0004](0004-docker-container-packaging.md)) and Aspire
  integration for local orchestration.
- **Trade-off:** .NET 10 is a recent release; contributors must install the matching SDK (pinned via the
  Docker `sdk:10.0` image and `TargetFramework`). Divergence from Pulumi's Go codebase means we port behavior
  by reading the spec and CLI client, not by sharing code.
- **Neutral:** All four solution projects target `net10.0` uniformly.
