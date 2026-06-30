#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// Pulumi stack permission levels, as they appear on the wire (numeric <c>permission</c> fields). The
/// real service uses the same magnitudes: 0 = none, 101 = read, 102 = write, 103 = admin.
/// </summary>
public static class StackPermissionLevel
{
    public const long None = 0;
    public const long Read = 101;
    public const long Write = 102;
    public const long Admin = 103;
}

/// <summary>
/// Persistence seam for per-stack access grants (ADR-0005): a user or team mapped to a permission level on
/// a single stack. Backs the collaborators / teams / member-stack-permission surfaces. The default is
/// in-memory like the other stores; a PostgreSQL implementation drops in behind this interface. All
/// operations are safe for concurrent use.
/// </summary>
public interface IStackPermissionStore
{
    // Users -----------------------------------------------------------------
    /// <summary>Grants (or overwrites) a user's explicit permission on the stack. <paramref name="isCreator"/>
    /// marks the grant as the stack creator's (reported by <see cref="GetStackCreator"/>).</summary>
    void SetUserPermission(StackCoordinates coordinates, string userName, long permission, bool isCreator = false);

    /// <summary>The stack's explicit user collaborators and their permission levels.</summary>
    IReadOnlyList<(string Name, long Permission)> ListUsers(StackCoordinates coordinates);

    /// <summary>Removes a user's explicit grant. Returns false when the user had no grant on the stack.</summary>
    bool RemoveUser(StackCoordinates coordinates, string userName);

    /// <summary>A user's explicit permission on the stack, or null when none is granted.</summary>
    long? GetUserPermission(StackCoordinates coordinates, string userName);

    /// <summary>The login of the stack's creator, or null when no creator grant is recorded.</summary>
    string? GetStackCreator(StackCoordinates coordinates);

    // Teams -----------------------------------------------------------------
    /// <summary>The teams with a grant on the stack and their permission levels.</summary>
    IReadOnlyList<(string TeamName, long Permission)> ListTeams(StackCoordinates coordinates);

    /// <summary>Grants (or overwrites) a team's permission on the stack.</summary>
    void SetTeamPermission(StackCoordinates coordinates, string teamName, long permission);

    /// <summary>Removes a team's grant. Returns false when the team had no grant on the stack.</summary>
    bool RemoveTeam(StackCoordinates coordinates, string teamName);
}
