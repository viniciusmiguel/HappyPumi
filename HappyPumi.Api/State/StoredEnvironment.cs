#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>Identifies an ESC environment.</summary>
public readonly record struct EnvCoordinates(string Org, string Project, string Name);

/// <summary>An ESC environment with its current definition and metadata.</summary>
public sealed class StoredEnvironment
{
    public required EnvCoordinates Coordinates { get; init; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public string OwnerLogin { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public bool DeletionProtected { get; set; }
    public string Yaml { get; set; } = "values: {}\n";
    public long CurrentRevision { get; set; } = 1;
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>One revision of an environment's definition.</summary>
public sealed class StoredEnvRevision
{
    public required long Number { get; init; }
    public DateTime Created { get; set; }
    public string CreatorLogin { get; set; } = "";
    public string CreatorName { get; set; } = "";
    public string Yaml { get; set; } = "";
    public List<string> Tags { get; set; } = new();
}
