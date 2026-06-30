#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A recorded webhook delivery attempt. Key: Id. Scope is stored as two scalar columns (kind + key) so a
/// scope's deliveries can be queried directly; bodies are text. Shared across webhook scopes (stack/org/env).
/// </summary>
public sealed class WebhookDeliveryRow
{
    public string Id { get; set; } = default!;
    public string ScopeKind { get; set; } = default!;
    public string ScopeId { get; set; } = default!;
    public string WebhookName { get; set; } = default!;
    public string Event { get; set; } = default!;
    public string RequestBody { get; set; } = "";
    public int ResponseStatus { get; set; }
    public string? ResponseBody { get; set; }
    public long DurationMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
