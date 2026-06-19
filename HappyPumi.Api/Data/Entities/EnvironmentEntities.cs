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

    /// <summary>Soft-delete: a deleted environment is hidden but restorable within the retention window.</summary>
    public bool Deleted { get; set; }
    public DateTime? DeletedAt { get; set; }
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

    /// <summary>Retraction marks a revision withdrawn (kept in history but no longer a valid version to use).</summary>
    public bool Retracted { get; set; }
    public DateTime? RetractedAt { get; set; }
    public string? RetractedByLogin { get; set; }
    public string? RetractedByName { get; set; }
    public string? RetractReason { get; set; }
    /// <summary>Optional revision number recommended in place of the retracted one.</summary>
    public long? RetractReplacement { get; set; }
}

/// <summary>An environment webhook. Key: Id; unique per (Org, Project, EnvName, Name).</summary>
public sealed class EnvironmentWebhookRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string EnvName { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = "";
    public string PayloadUrl { get; set; } = "";
    public bool Active { get; set; } = true;
    public string? Format { get; set; }
    public string? Secret { get; set; }
    /// <summary>Event filters (jsonb).</summary>
    public List<string> Filters { get; set; } = new();
    /// <summary>Permission groups the delivery runs as (jsonb).</summary>
    public List<string> Groups { get; set; } = new();
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
