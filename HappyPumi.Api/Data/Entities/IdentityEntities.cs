#nullable enable

using System;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Data.Entities;

/// <summary>An org membership. Key: (Org, UserLogin).</summary>
public sealed class MemberRow
{
    public string Org { get; set; } = default!;
    public string UserLogin { get; set; } = default!;
    public string Role { get; set; } = default!;
    public DateTime Created { get; set; }
}

/// <summary>A custom org role. Key: Id. The permission descriptor is jsonb.</summary>
public sealed class RoleRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public int Version { get; set; }
    public bool IsOrgDefault { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ResourceType { get; set; }
    public string? UxPurpose { get; set; }
    public PermissionDescriptor? Details { get; set; }
}

/// <summary>A team's grant of a role. Key: (Org, TeamName, RoleId).</summary>
public sealed class TeamRoleRow
{
    public string Org { get; set; } = default!;
    public string TeamName { get; set; } = default!;
    public string RoleId { get; set; } = default!;
}

/// <summary>An org team. Key: (Org, Name). Members are jsonb (a list of user logins).</summary>
public sealed class TeamRow
{
    public string Org { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Kind { get; set; } = "pulumi";
    public System.Collections.Generic.List<string> Members { get; set; } = new();
}
