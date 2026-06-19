#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>One published (or pending) version of a registry package (ENDPOINTS.md 4).</summary>
public sealed class StoredPackageVersion
{
    public required PackageCoordinates Coordinates { get; init; }
    public required string Version { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }

    /// <summary>False between StartPublish and CompletePublish (the upload window); true once finalized.</summary>
    public bool Published { get; set; }

    /// <summary>README markdown for the Overview tab.</summary>
    public string? Readme { get; set; }
    /// <summary>API-docs navigation tree (modules → resources/functions).</summary>
    public List<GetPackageNavModule>? Nav { get; set; }
}
