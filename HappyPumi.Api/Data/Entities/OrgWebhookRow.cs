#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// An organization-level webhook definition. Key: Id; unique on (Org, Name). Filters/Groups are jsonb. The
/// secret is stored for HMAC signing and only its presence is exposed to clients (sanitized at the endpoint).
/// </summary>
public sealed class OrgWebhookRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = "";
    public string PayloadUrl { get; set; } = "";
    public bool Active { get; set; } = true;
    public string? Format { get; set; }
    public string? Secret { get; set; }
    /// <summary>Event filters (jsonb).</summary>
    public List<string>? Filters { get; set; }
    /// <summary>Event groups (jsonb).</summary>
    public List<string>? Groups { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
