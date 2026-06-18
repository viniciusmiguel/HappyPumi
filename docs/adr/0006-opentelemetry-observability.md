# ADR-0006 — OpenTelemetry for telemetry and logging

**Status:** Accepted
**Date:** 2026-06-18

## Context

HappyPumi drives long-running, multi-step flows — the update lifecycle spans create → start → lease renewal →
engine-event streaming → checkpointing → complete, often across minutes. Diagnosing failures and latency in
those flows requires correlated traces, metrics, and logs, not just ad-hoc logging. We also want to avoid
coupling to any single observability vendor, and we already run under .NET Aspire
([ADR-0004](0004-docker-container-packaging.md)), whose dashboard speaks OpenTelemetry natively.

## Decision

Use **OpenTelemetry** as the single standard for traces, metrics, and logs.

- Instrument `HappyPumi.Api` with the OpenTelemetry .NET SDK: ASP.NET Core / HTTP instrumentation out of the
  box, plus custom spans around the update lifecycle and database calls.
- Logs flow through `Microsoft.Extensions.Logging` exported via the OpenTelemetry logging provider, so logs,
  traces, and metrics share trace/span correlation IDs.
- Export via **OTLP** to a configurable collector endpoint. In local dev that endpoint is the **Aspire
  dashboard**; in production it points at the operator's chosen backend (Collector, Jaeger, Prometheus,
  Grafana, vendor, etc.).
- Centralize setup in a shared ServiceDefaults-style configuration so every service is instrumented uniformly.

## Consequences

- **Positive:** Vendor-neutral and standards-based — operators self-hosting HappyPumi can plug in whatever
  backend they run. Zero extra wiring to get rich local observability via the Aspire dashboard.
- **Positive:** Correlated traces across the update lifecycle make the most complex flows debuggable; metrics
  give SLO/latency visibility.
- **Trade-off:** Instrumentation and exporter overhead; sampling/retention must be tuned, especially around
  high-volume engine-event and checkpoint endpoints.
- **Follow-up:** Decide span/metric naming conventions and sampling strategy; ensure secrets and decrypted
  config never land in span attributes or logs (PII/secret-redaction is a hard requirement given the
  encrypt/decrypt endpoints).
