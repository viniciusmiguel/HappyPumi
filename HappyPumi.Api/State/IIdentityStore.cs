#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for the per-org IDP model — members, roles, and team-role assignments (ADR-0007).
/// In-memory by default like the other stores (ADR-0005); orgs are created on first reference. Safe for
/// concurrent use.
/// </summary>
public interface IIdentityStore
{
    // Members ---------------------------------------------------------------
    IReadOnlyCollection<StoredMember> ListMembers(string org);

    /// <summary>Adds or overwrites a member, returning the stored membership.</summary>
    StoredMember AddMember(string org, string userLogin, string role);

    /// <summary>Updates a member's role. Returns the member, or null when not a member.</summary>
    StoredMember? UpdateMemberRole(string org, string userLogin, string role);

    /// <summary>
    /// Members holding <paramref name="roleId"/>. A member's <see cref="StoredMember.Role"/> string may carry
    /// either a role's opaque Id or its Name, so this resolves the role and matches both. Empty when the role
    /// is missing or unassigned.
    /// </summary>
    IReadOnlyList<StoredMember> ListMembersWithRole(string org, string roleId);

    /// <summary>Removes a member. Returns false when not a member.</summary>
    bool RemoveMember(string org, string userLogin);

    // Roles -----------------------------------------------------------------
    IReadOnlyCollection<StoredRole> ListRoles(string org);
    StoredRole CreateRole(string org, PermissionDescriptorBase descriptor);
    StoredRole? GetRole(string org, string roleId);
    StoredRole? UpdateRole(string org, string roleId, PermissionDescriptorBase descriptor);
    bool DeleteRole(string org, string roleId);

    /// <summary>
    /// Marks <paramref name="roleId"/> as the org's default role (new members inherit it), clearing the flag on
    /// every other role in the org. Returns false when the role is missing.
    /// </summary>
    bool SetDefaultRole(string org, string roleId);

    // Teams -----------------------------------------------------------------
    IReadOnlyCollection<StoredTeam> ListTeams(string org);
    StoredTeam? GetTeam(string org, string teamName);

    /// <summary>Creates a team. Returns null when one of that name already exists.</summary>
    StoredTeam? CreateTeam(string org, string name, string displayName, string description, string kind);

    /// <summary>
    /// Renames and/or re-describes a team. Non-null <paramref name="newName"/> renames the team (carrying its
    /// members and role grants); null fields are left unchanged. Returns the updated team, or null when the
    /// team is missing or <paramref name="newName"/> collides with an existing team.
    /// </summary>
    StoredTeam? UpdateTeam(string org, string teamName, string? newName, string? displayName, string? description);

    bool DeleteTeam(string org, string teamName);

    /// <summary>Adds a member to a team. Returns false when the team does not exist.</summary>
    bool AddTeamMember(string org, string teamName, string userLogin);

    /// <summary>Removes a member from a team. Returns false when the team or membership is absent.</summary>
    bool RemoveTeamMember(string org, string teamName, string userLogin);

    // Team role assignments -------------------------------------------------
    /// <summary>Grants a role to a team. Returns false when the role does not exist in the org.</summary>
    bool AssignTeamRole(string org, string teamName, string roleId);

    /// <summary>Revokes a role from a team. Returns false when the team did not hold the role.</summary>
    bool RemoveTeamRole(string org, string teamName, string roleId);
}
