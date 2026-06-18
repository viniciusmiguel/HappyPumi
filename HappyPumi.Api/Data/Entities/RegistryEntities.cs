#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

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
    /// <summary>README markdown (served as text to the console's Overview tab).</summary>
    public string? Readme { get; set; }
    /// <summary>API-docs navigation tree (modules → resources/functions); jsonb.</summary>
    public List<GetPackageNavModule>? Nav { get; set; }
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
