#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IIdentityStore"/> (ADR-0005). One <see cref="OrgState"/> per org, created lazily.</summary>
public sealed class InMemoryIdentityStore : IIdentityStore
{
    private sealed class OrgState
    {
        public ConcurrentDictionary<string, StoredMember> Members { get; } = new();
        public ConcurrentDictionary<string, StoredRole> Roles { get; } = new();
        public ConcurrentDictionary<string, HashSet<string>> TeamRoles { get; } = new();
    }

    private readonly ConcurrentDictionary<string, OrgState> _orgs = new();

    private OrgState Org(string org) => _orgs.GetOrAdd(org, _ => new OrgState());

    public IReadOnlyCollection<StoredMember> ListMembers(string org) => Org(org).Members.Values.ToArray();

    public StoredMember AddMember(string org, string userLogin, string role)
    {
        var member = new StoredMember { UserLogin = userLogin, Role = role };
        Org(org).Members[userLogin] = member;
        return member;
    }

    public StoredMember? UpdateMemberRole(string org, string userLogin, string role)
    {
        if (!Org(org).Members.TryGetValue(userLogin, out var member))
            return null;
        member.Role = role;
        return member;
    }

    public bool RemoveMember(string org, string userLogin) => Org(org).Members.TryRemove(userLogin, out _);

    public IReadOnlyCollection<StoredRole> ListRoles(string org) => Org(org).Roles.Values.ToArray();

    public StoredRole CreateRole(string org, PermissionDescriptorBase descriptor)
    {
        var role = new StoredRole { Id = Guid.NewGuid().ToString(), OrgId = org };
        role.Apply(descriptor);
        Org(org).Roles[role.Id] = role;
        return role;
    }

    public StoredRole? GetRole(string org, string roleId)
        => Org(org).Roles.TryGetValue(roleId, out var role) ? role : null;

    public StoredRole? UpdateRole(string org, string roleId, PermissionDescriptorBase descriptor)
    {
        if (!Org(org).Roles.TryGetValue(roleId, out var role))
            return null;
        role.Apply(descriptor);
        role.Modified = DateTime.UtcNow;
        role.Version++;
        return role;
    }

    public bool DeleteRole(string org, string roleId)
    {
        if (!Org(org).Roles.TryRemove(roleId, out _))
            return false;
        foreach (var roles in Org(org).TeamRoles.Values)
            lock (roles) roles.Remove(roleId); // drop dangling team grants
        return true;
    }

    public bool AssignTeamRole(string org, string teamName, string roleId)
    {
        if (!Org(org).Roles.ContainsKey(roleId))
            return false;
        var roles = Org(org).TeamRoles.GetOrAdd(teamName, _ => new HashSet<string>());
        lock (roles) roles.Add(roleId);
        return true;
    }

    public bool RemoveTeamRole(string org, string teamName, string roleId)
    {
        if (!Org(org).TeamRoles.TryGetValue(teamName, out var roles))
            return false;
        lock (roles) return roles.Remove(roleId);
    }
}
