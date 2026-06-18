#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>A package version. Key: (Source, Publisher, Name, Version).</summary>
public sealed class PackageVersionRow
{
    public string Source { get; set; } = default!;
    public string Publisher { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Version { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public bool Published { get; set; }
}

/// <summary>A template version. Key: (Source, Publisher, Name, Version).</summary>
public sealed class TemplateVersionRow
{
    public string Source { get; set; } = default!;
    public string Publisher { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Version { get; set; } = default!;
    public DateTime UpdatedAt { get; set; }
    public string? Language { get; set; }
    public string? Description { get; set; }
    public bool Published { get; set; }
}
