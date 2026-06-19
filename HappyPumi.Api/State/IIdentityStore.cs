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

    /// <summary>Removes a member. Returns false when not a member.</summary>
    bool RemoveMember(string org, string userLogin);

    // Roles -----------------------------------------------------------------
    IReadOnlyCollection<StoredRole> ListRoles(string org);
    StoredRole CreateRole(string org, PermissionDescriptorBase descriptor);
    StoredRole? GetRole(string org, string roleId);
    StoredRole? UpdateRole(string org, string roleId, PermissionDescriptorBase descriptor);
    bool DeleteRole(string org, string roleId);

    // Teams -----------------------------------------------------------------
    IReadOnlyCollection<StoredTeam> ListTeams(string org);
    StoredTeam? GetTeam(string org, string teamName);

    /// <summary>Creates a team. Returns null when one of that name already exists.</summary>
    StoredTeam? CreateTeam(string org, string name, string displayName, string description, string kind);

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
