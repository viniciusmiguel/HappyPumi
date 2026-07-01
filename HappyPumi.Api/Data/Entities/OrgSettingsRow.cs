#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// Persisted per-org settings backing GetOrganization/UpdateOrganizationSettings. Key: Org (one row per org).
/// </summary>
public sealed class OrgSettingsRow
{
    public string Org { get; set; } = default!;
    public bool MembersCanCreateStacks { get; set; } = true;
    public bool MembersCanDeleteStacks { get; set; } = true;
    public bool MembersCanCreateTeams { get; set; } = true;
    public bool MembersCanTransferStacks { get; set; } = true;
    public bool MembersCanCreateAccounts { get; set; } = true;
    public long DefaultStackPermission { get; set; }
    public long DefaultAccountPermission { get; set; }
    public string DefaultEnvironmentPermission { get; set; } = "none";
    public string? DefaultRoleId { get; set; }
    public string? DefaultDeploymentRoleId { get; set; }
    public string? DefaultAgentPoolId { get; set; }
    public string PreferredVcs { get; set; } = "github";
    public string AiEnablement { get; set; } = "disabled";
    public bool NeoEnabled { get; set; }
    public DateTime Created { get; set; }
}
