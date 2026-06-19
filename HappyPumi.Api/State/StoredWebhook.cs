#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>An environment webhook: where to POST on environment events, and what to filter. Domain shape.</summary>
public sealed class StoredWebhook
{
    public required string Name { get; init; }
    public string DisplayName { get; set; } = "";
    public string PayloadUrl { get; set; } = "";
    public bool Active { get; set; } = true;
    public string? Format { get; set; }
    /// <summary>Shared secret used to sign deliveries; stored, never echoed (only its presence is exposed).</summary>
    public string? Secret { get; set; }
    public List<string> Filters { get; set; } = new();
    public List<string> Groups { get; set; } = new();
    public DateTime Created { get; set; }
}
