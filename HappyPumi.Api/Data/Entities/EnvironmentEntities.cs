#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.Data.Entities;

/// <summary>An ESC environment. Key: (Org, Project, Name). The current definition is stored as YAML text.</summary>
public sealed class EnvironmentRow
{
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
    public string OwnerLogin { get; set; } = default!;
    public string OwnerName { get; set; } = default!;
    public bool DeletionProtected { get; set; }
    /// <summary>The current environment definition (ESC YAML).</summary>
    public string Yaml { get; set; } = "values: {}\n";
    public long CurrentRevision { get; set; } = 1;
    /// <summary>Environment-level tag map (jsonb).</summary>
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>An immutable revision of an environment's definition. Key: Id; indexed by (Org, Project, Name).</summary>
public sealed class EnvironmentRevisionRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long Number { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string CreatorLogin { get; set; } = default!;
    public string CreatorName { get; set; } = default!;
    public string Yaml { get; set; } = "";
    /// <summary>Revision tags (e.g. "latest", "stable"); jsonb.</summary>
    public List<string> Tags { get; set; } = new();
}
