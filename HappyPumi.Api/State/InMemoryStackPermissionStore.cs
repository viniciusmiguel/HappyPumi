#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>
/// In-memory <see cref="IStackPermissionStore"/> backed by a concurrent dictionary (ADR-0005). Default
/// store for local dev and tests; state is lost on restart. The PostgreSQL implementation replaces it
/// behind the interface without endpoint changes.
/// </summary>
public sealed class InMemoryStackPermissionStore : IStackPermissionStore
{
    private const string UserKind = "user";
    private const string TeamKind = "team";

    private sealed record Grant(long Permission, bool IsCreator);

    // Keyed by (coordinates, subject-kind, subject-name) so a user and a team may share a name.
    private readonly ConcurrentDictionary<(StackCoordinates, string, string), Grant> _grants = new();

    public void SetUserPermission(StackCoordinates coordinates, string userName, long permission, bool isCreator = false)
        => _grants[(coordinates, UserKind, userName)] = new Grant(permission, isCreator);

    public IReadOnlyList<(string Name, long Permission)> ListUsers(StackCoordinates coordinates)
        => Subjects(coordinates, UserKind).Select(g => (g.Key.Item3, g.Value.Permission)).ToList();

    public bool RemoveUser(StackCoordinates coordinates, string userName)
        => _grants.TryRemove((coordinates, UserKind, userName), out _);

    public long? GetUserPermission(StackCoordinates coordinates, string userName)
        => _grants.TryGetValue((coordinates, UserKind, userName), out var g) ? g.Permission : null;

    public string? GetStackCreator(StackCoordinates coordinates)
        => Subjects(coordinates, UserKind).Where(g => g.Value.IsCreator).Select(g => g.Key.Item3).FirstOrDefault();

    public IReadOnlyList<(string TeamName, long Permission)> ListTeams(StackCoordinates coordinates)
        => Subjects(coordinates, TeamKind).Select(g => (g.Key.Item3, g.Value.Permission)).ToList();

    public void SetTeamPermission(StackCoordinates coordinates, string teamName, long permission)
        => _grants[(coordinates, TeamKind, teamName)] = new Grant(permission, false);

    public bool RemoveTeam(StackCoordinates coordinates, string teamName)
        => _grants.TryRemove((coordinates, TeamKind, teamName), out _);

    private IEnumerable<KeyValuePair<(StackCoordinates, string, string), Grant>> Subjects(StackCoordinates c, string kind)
        => _grants.Where(g => g.Key.Item1 == c && g.Key.Item2 == kind);
}
