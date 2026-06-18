#nullable enable

using System;

namespace HappyPumi.Api.State;

/// <summary>The registry identity of a template: <c>source/publisher/name</c>.</summary>
public readonly record struct TemplateCoordinates(string Source, string Publisher, string Name);

/// <summary>One published (or pending) version of a registry template (ENDPOINTS.md 4).</summary>
public sealed class StoredTemplateVersion
{
    public required TemplateCoordinates Coordinates { get; init; }
    public required string Version { get; init; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Language { get; set; }
    public string? Description { get; set; }

    /// <summary>False between StartPublish and CompletePublish; true once finalized.</summary>
    public bool Published { get; set; }
}
