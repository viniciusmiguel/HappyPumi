#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// The scope a webhook delivery belongs to: a kind (<c>stack</c>|<c>org</c>|<c>environment</c>) and the
/// scope key within that kind (e.g. <c>org/proj/stack</c> for a stack, <c>org</c> for an organization).
/// Used to partition deliveries so each scope's endpoints only see their own history.
/// </summary>
public sealed record WebhookScope(string Kind, string Id);

/// <summary>
/// One recorded webhook delivery attempt: what was sent, where, and the outcome. A delivery is recorded
/// even when the POST fails or is blocked (<see cref="ResponseStatus"/> 0), so the history is complete.
/// </summary>
public sealed class StoredWebhookDelivery
{
    public required string Id { get; init; }
    public required WebhookScope Scope { get; init; }
    public required string WebhookName { get; init; }
    public required string Event { get; init; }
    public string RequestBody { get; set; } = "";

    /// <summary>The HTTP response status; 0 means the delivery was not sent (network error or SSRF-blocked).</summary>
    public int ResponseStatus { get; set; }
    public string? ResponseBody { get; set; }
    public long DurationMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persistence seam for webhook delivery history, partitioned by (scope, webhook name). In-memory by
/// default (ADR-0005), Postgres in production. Shared by every webhook scope (stack/org/env).
/// </summary>
public interface IWebhookDeliveryStore
{
    /// <summary>Records a delivery and returns it.</summary>
    StoredWebhookDelivery Append(StoredWebhookDelivery delivery);

    /// <summary>All deliveries for one webhook within a scope, newest first.</summary>
    IReadOnlyList<StoredWebhookDelivery> List(WebhookScope scope, string webhookName);

    /// <summary>The newest delivery of a given event for one webhook, or null when there is none (redeliver).</summary>
    StoredWebhookDelivery? LatestByEvent(WebhookScope scope, string webhookName, string @event);
}
