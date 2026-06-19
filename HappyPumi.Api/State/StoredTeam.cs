#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>An org team (ENDPOINTS.md 4 — RBAC). Holds membership and assigned role ids; role assignments
/// are also reflected in the identity store's team-role map.</summary>
public sealed class StoredTeam
{
    public required string Name { get; init; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>"pulumi" (managed here) or "github" (mirrored from a VCS team).</summary>
    public string Kind { get; set; } = "pulumi";
    public List<string> Members { get; } = new();
    public List<string> RoleIds { get; } = new();
}
