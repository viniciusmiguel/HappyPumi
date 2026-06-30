# Webhooks (org + stack + ESC environment) — design

> Status: approved (brainstorm) · 2026-06-30 · phased across 3 PRs

## Goal

Implement HappyPumi's webhook surface for all three scopes — **organization, stack, and ESC
environment** (22 currently-`NotImplementedException` endpoints) — with **real, event-fired
delivery**: a shared dispatcher POSTs to the payload URL on actual events, signs with HMAC,
renders the configured format, and records each delivery; ping and redeliver perform real POSTs.

## Decisions (from brainstorm)

- **Full event-fired delivery** (not just CRUD): wire firing into the update/deployment/env
  mutation points, alongside the existing `IAuditLog.Record` emission (ADR-0010).
- **All 22 endpoints**: org (8) + stack (6 remaining) + ESC environment (8).
- **3 phased PRs**, each merged when CI is green.

## Existing building blocks

- `State/StoredWebhook.cs` — shared config model `{ Name, DisplayName, PayloadUrl, Active, Format,
  Secret, Filters, Groups, Created }`. `Contracts/WebhookResponse.cs` is the wire shape.
- Stack webhooks: `ListStackWebhooks`/`CreateStackWebhook` already implemented via
  `IDeploymentStore` (`AddWebhook`/`ListWebhooks`).
- ESC env webhooks: `IEnvironmentWebhookStore` already has full CRUD (List/Get/Create/Update/Delete).
- Org webhooks: no store yet (new in PR2).

## Architecture

### Shared dispatcher core (`HappyPumi.Api/Webhooks/`, built in PR1, reused by PR2/PR3)
- **`WebhookDispatcher.FireAsync(webhooks, eventType, payload)`** — for each active webhook whose
  `Filters` match `eventType`: render the body via the format formatter, compute an
  `X-Pulumi-Signature` HMAC-SHA256 over the body with the webhook `Secret`, POST via an injected
  typed `HttpClient`, and record a `WebhookDelivery`. Failures are recorded, never thrown.
- **`IWebhookDeliveryStore`** (new, in-memory + Postgres `jsonb`): append, list-by-webhook,
  get-latest-by-event (for redeliver). `WebhookDelivery { Id, Scope, WebhookName, Event,
  RequestBody, ResponseStatus, ResponseBody, DurationMs, Timestamp }`.
- **`IWebhookPayloadFormatter`** map keyed by format: `raw`, `slack`, `ms_teams`,
  `pulumi_deployments`. Default `raw`.
- **Filter matching** on event-type strings (e.g. `stack_update`, `deployment_*`, `env_*`).
- **Ping** POSTs a synthetic ping event; **redeliver** re-POSTs a stored delivery's payload — both
  real, both recorded.
- **SSRF safety:** payload URLs are user-controlled and we POST to them (product parity). Default
  permissive, with a config deny-list `Webhooks:BlockedHosts` (e.g. cloud metadata IPs) so a
  hardened deployment can restrict targets. The dispatcher checks the resolved host against it
  before POSTing; a blocked target is recorded as a failed delivery.
- DI: register the dispatcher, delivery store, formatters, and a typed `HttpClient` in `Program.cs`.

### Per-scope config + endpoints
- **Stack (PR1):** extend `IDeploymentStore` with `GetWebhook`/`UpdateWebhook`/`DeleteWebhook`
  (it already has add/list). Implement `GetStackWebhook`, `UpdateStackWebhook`,
  `DeleteStackWebhook`, `GetStackWebhookDeliveries`, `RedeliverStackWebhookEvent`,
  `PingStackWebhook`.
- **Org (PR2):** new `IOrgWebhookStore` (in-memory + Postgres). Implement `ListOrganizationWebhooks`,
  `CreateOrganizationWebhook`, `GetOrganizationWebhook`, `UpdateOrganizationWebhook`,
  `DeleteOrganizationWebhook`, `GetOrganizationWebhookDeliveries`, `RedeliverOrganizationWebhookEvent`,
  `PingOrganizationWebhook`.
- **ESC environment (PR3):** use the existing `IEnvironmentWebhookStore`. Implement
  `ListWebhooksPreviewEnvironments`, `CreateWebhookPreviewEnvironments`, `GetWebhookPreviewEnvironments`,
  `UpdateWebhookPreviewEnvironments`, `DeleteWebhookPreviewEnvironments`,
  `GetWebhookDeliveriesPreviewEnvironments`, `RedeliverWebhookEventPreviewEnvironments`,
  `PingWebhookPreviewEnvironments`.

### Event wiring (firing)
- **Stack (PR1):** fire `stack_update` on `CompleteUpdate*` completion; fire `deployment_*` on the
  deployment status transition (`IDeploymentQueue.SetStatusByJobId`). Co-locate with the existing
  audit emission so the trigger points stay together.
- **Org (PR2):** fire on representative org events at their mutation points.
- **Env (PR3):** fire on env mutation/open events.

## Frontend
- **Stack (PR1):** a Webhooks section in the stack **Settings** tab — list/add/edit/delete, plus a
  deliveries view with redeliver + ping.
- **Org (PR2):** a new org-settings **Webhooks** page.
- **Env (PR3):** wire the ESC env-detail **Webhooks** tab to the real `/preview/environments/.../hooks`
  endpoints.
- Reuse `Card`/`Table`/`Modal`/`Field`/`Badge`; split into `…/webhooks` components if files approach 500 lines.

## Testing & process
- Dispatcher/formatter/HMAC **unit tests**; outbound POST shape (URL, `X-Pulumi-Signature`, formatted
  body) asserted via the ESC `StubHttpHandler` (no network).
- **Component tests** (`HappyPumi.Api.Tests`, real Postgres): CRUD per scope; deliveries recorded;
  redeliver/ping; an **event-fire test** (complete an update → a matching stack webhook records a
  delivery, using a stub handler).
- Config-gated: runs without external targets (deliveries to unreachable/blocked URLs recorded as
  failed, never fatal).
- Console `npm run build && npm run lint` per PR. Coverage > 80% changed C#, duplication < 3%.
- Each PR branches off `main`, granular commits (Co-Authored-By trailer), merged when CI green. No
  squash. Edit only `HandleAsync` bodies in `// <auto-generated />` endpoint files; never contract
  files (the generator recursive-`Body` fix is on `main`, but already-committed stubs may still carry
  it — use the `Contracts.X` / `EndpointWithoutRequest` workaround if hit, and report).

## Acceptance
- All 22 endpoints return real data / perform their action (no `NotImplementedException`), covered by
  tests; a real event fires a recorded delivery; ping/redeliver POST for real; the console manages
  webhooks at all three scopes and builds + lints clean.

## This PR (PR1 — webhook core + stack webhooks)
The shared dispatcher / `IWebhookDeliveryStore` / formatters / HMAC / SSRF guard; `IDeploymentStore`
get/update/delete-webhook; the 6 stack webhook endpoints; firing on stack-update-complete + deployment
status change; console stack Settings webhooks section. The design doc lands with this PR.
