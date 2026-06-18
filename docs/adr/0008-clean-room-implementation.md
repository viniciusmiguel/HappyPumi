# ADR-0008 — Clean-room implementation from public sources only

**Status:** Accepted
**Date:** 2026-06-18

## Context

HappyPumi reimplements the Pulumi Cloud API. Pulumi Cloud's **server-side** source code is proprietary and is
**not** available to this project. To keep HappyPumi legally clean and independently distributable as
open source, we must be explicit and disciplined about which inputs the implementation is allowed to derive
from.

Two legitimate, public inputs exist:

1. **The Pulumi Cloud OpenAPI specification** (`pulumi-spec.json`) — the publicly published HTTP contract.
2. **The Pulumi CLI** (`github.com/pulumi/pulumi`) — open source under the **Apache License 2.0**, which
   permits use, study, and derivation with attribution and notice.

## Decision

HappyPumi is a **clean-room implementation** derived **solely** from:

- the **public OpenAPI spec** (`pulumi-spec.json`) — the source of truth for routes, verbs, and schemas; and
- the **Apache-2.0-licensed Pulumi CLI** — used only to observe how a real client calls the API (request
  sequences, expected status codes, the update lifecycle) via its public HTTP client code.

We **do not** reference, copy, decompile, or otherwise use Pulumi Cloud's proprietary server-side source code,
internal documentation, or any non-public material. All server behavior is inferred from the public contract
and the observable client behavior, then implemented independently.

Because the Pulumi CLI is Apache-2.0, any insight drawn from it is used in compliance with that license
(attribution / NOTICE as applicable). HappyPumi's own server code is original work.

## Consequences

- **Positive:** A defensible clean-room provenance — HappyPumi can be released as open source without
  entanglement with Pulumi Cloud's proprietary server code.
- **Positive:** The constraint reinforces the spec-driven workflow already in place (generator + `ENDPOINTS.md`):
  the spec and CLI are the *only* inputs, so they are the only things we need to keep in sync with.
- **Obligation:** Comply with Apache-2.0 for anything derived from the CLI — preserve attribution/NOTICE and
  do not claim Pulumi endorsement. Pulumi trademarks are not used beyond nominative/descriptive reference.
- **Constraint on contributors:** Do not introduce knowledge from Pulumi Cloud's proprietary internals
  (e.g. leaked source, private docs). If a behavior can't be determined from the public spec or the
  Apache-2.0 CLI, infer it from first principles and document the assumption — don't source it from
  non-public material.
