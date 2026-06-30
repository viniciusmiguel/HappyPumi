#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Data.Entities;

/// <summary>A stack row. Key: (Org, Project, Stack). Nested config/checkpoint are jsonb.</summary>
public sealed class StackRow
{
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Stack { get; set; } = default!;
    public long Version { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public AppStackConfig? Config { get; set; }
    public AppUntypedDeployment? Deployment { get; set; }
}

/// <summary>One completed update in a stack's history. Key: UpdateId. Ordered by Version.</summary>
public sealed class StackUpdateRow
{
    public string UpdateId { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Stack { get; set; } = default!;
    public long Version { get; set; }
    public string Kind { get; set; } = default!;
    public string Result { get; set; } = default!;
    public string Message { get; set; } = string.Empty;
    public long StartTime { get; set; }
    public long EndTime { get; set; }
    public Dictionary<string, AppConfigValue> Config { get; set; } = new();
    /// <summary>The user who requested the update (null for history recorded before the actor was captured).</summary>
    public string? RequestedByLogin { get; set; }
    public string? RequestedByName { get; set; }
}

/// <summary>
/// A per-stack access grant: a user or team mapped to a permission level on one stack. Key:
/// (Org, Project, Stack, SubjectKind, SubjectName). <see cref="IsCreator"/> flags the stack-creator grant.
/// </summary>
public sealed class StackPermissionRow
{
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Stack { get; set; } = default!;
    /// <summary>"user" or "team" — distinguishes the two subject namespaces sharing a name.</summary>
    public string SubjectKind { get; set; } = default!;
    public string SubjectName { get; set; } = default!;
    public long Permission { get; set; }
    public bool IsCreator { get; set; }
}

/// <summary>An in-flight/finished update operation (the lifecycle state). Key: UpdateId.</summary>
public sealed class UpdateRow
{
    public string UpdateId { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Stack { get; set; } = default!;
    public string Kind { get; set; } = default!;
    public bool DryRun { get; set; }
    public string Status { get; set; } = default!;
    public string Token { get; set; } = string.Empty;
    public long Version { get; set; }
    public long StartedAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RequestedByLogin { get; set; }
    public string? RequestedByName { get; set; }
    public Dictionary<string, AppConfigValue>? Config { get; set; }
    public AppUntypedDeployment? Checkpoint { get; set; }

    /// <summary>Engine events recorded by the CLI for this update, in arrival order (jsonb).</summary>
    public List<AppEngineEvent> Events { get; set; } = new();
}
