#nullable enable

using System;

namespace HappyPumi.Api.State;

/// <summary>
/// The persisted, org-wide settings that back <c>GetOrganization</c>/<c>UpdateOrganizationSettings</c>
/// (ADR-0005). Sensible defaults match a fresh Pulumi org (members may create/delete/transfer stacks and
/// create teams/accounts; GitHub is the preferred VCS; AI/Neo off).
/// </summary>
public sealed class StoredOrgSettings
{
    public required string Org { get; init; }
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
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persistence seam for per-org settings (ADR-0005). In-memory by default like the other stores; the
/// Postgres implementation persists one row per org. Safe for concurrent use.
/// </summary>
public interface IOrgSettingsStore
{
    /// <summary>Returns the org's settings, or a fresh default set (not persisted) when none exist yet.</summary>
    StoredOrgSettings Get(string org);

    /// <summary>Applies <paramref name="mutate"/> to the org's settings, persisting the result (creating the
    /// row on first write), and returns the updated settings.</summary>
    StoredOrgSettings Update(string org, Action<StoredOrgSettings> mutate);
}
