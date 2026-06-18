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
    public Dictionary<string, AppConfigValue>? Config { get; set; }
    public AppUntypedDeployment? Checkpoint { get; set; }
}
