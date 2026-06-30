#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for organization-level webhooks (one per (org, name)). Mirrors the stack webhook methods
/// on <see cref="IDeploymentStore"/> at the org scope. Carries the stored <c>secret</c> for signing — callers
/// must sanitize (<see cref="StackWebhookMapper.Sanitized"/>) before echoing to clients. Backed by PostgreSQL
/// in production and an in-memory map in unit tests (ADR-0005).
/// </summary>
public interface IOrgWebhookStore
{
    IReadOnlyList<WebhookResponse> List(string org);
    WebhookResponse? Get(string org, string name);
    /// <summary>Creates a webhook; null when one with the same name already exists in the org (→ 409).</summary>
    WebhookResponse? Create(string org, WebhookResponse webhook);
    /// <summary>Applies a PATCH body to an org webhook and returns the updated record; null when it does not exist.</summary>
    WebhookResponse? Update(string org, string name, Webhook patch);
    /// <summary>Removes an org webhook by name. False when no such webhook exists.</summary>
    bool Delete(string org, string name);
}
