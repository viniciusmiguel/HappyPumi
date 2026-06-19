#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IPolicyStore"/> (ADR-0005). Groups and packs are keyed by name per org.</summary>
public sealed class InMemoryPolicyStore : IPolicyStore
{
    private sealed class OrgPolicies
    {
        public ConcurrentDictionary<string, StoredPolicyGroup> Groups { get; } = new();
        public ConcurrentDictionary<string, StoredPolicyPack> Packs { get; } = new();
    }

    private readonly ConcurrentDictionary<string, OrgPolicies> _orgs = new();

    private OrgPolicies Org(string org) => _orgs.GetOrAdd(org, _ => new OrgPolicies());

    public IReadOnlyCollection<StoredPolicyGroup> ListGroups(string org) => Org(org).Groups.Values.ToArray();

    public StoredPolicyGroup? GetGroup(string org, string name)
        => Org(org).Groups.TryGetValue(name, out var g) ? g : null;

    public StoredPolicyGroup? NewGroup(string org, string name)
    {
        var group = new StoredPolicyGroup { Name = name };
        return Org(org).Groups.TryAdd(name, group) ? group : null;
    }

    public bool RenameGroup(string org, string name, string newName)
    {
        var groups = Org(org).Groups;
        if (!groups.TryGetValue(name, out var group) || groups.ContainsKey(newName))
            return false;
        group.Name = newName;
        return groups.TryAdd(newName, group) && groups.TryRemove(name, out _);
    }

    public bool DeleteGroup(string org, string name) => Org(org).Groups.TryRemove(name, out _);

    public bool AddPackToGroup(string org, string group, string packName)
    {
        var g = Org(org).Groups.GetOrAdd(group,
            n => new StoredPolicyGroup { Name = n, IsOrgDefault = n == "default-policy-group" });
        lock (g.AppliedPolicyPacks)
            if (!g.AppliedPolicyPacks.Contains(packName))
                g.AppliedPolicyPacks.Add(packName);
        return true;
    }

    public bool RemovePackFromGroup(string org, string group, string packName)
    {
        if (!Org(org).Groups.TryGetValue(group, out var g))
            return false;
        lock (g.AppliedPolicyPacks)
            return g.AppliedPolicyPacks.Remove(packName);
    }

    public IReadOnlyCollection<StoredPolicyPack> ListPacks(string org) => Org(org).Packs.Values.ToArray();

    public long CreatePackVersion(string org, string name, string displayName, List<AppPolicy>? policies, string? versionTag = null)
    {
        var pack = Org(org).Packs.GetOrAdd(name, n => new StoredPolicyPack { Name = n });
        pack.DisplayName = displayName;
        long version;
        lock (pack.Versions)
        {
            version = pack.Versions.Count == 0 ? 1 : pack.Versions.Keys.Max() + 1;
            pack.Versions[version] = new StoredPolicyPackVersion { Version = version, Policies = policies, VersionTag = versionTag };
        }
        return version;
    }

    public StoredPolicyPack? GetPack(string org, string name)
        => Org(org).Packs.TryGetValue(name, out var p) ? p : null;

    public bool CompletePack(string org, string name, long version)
    {
        if (!Org(org).Packs.TryGetValue(name, out var pack))
            return false;
        lock (pack.Versions)
        {
            if (!pack.Versions.TryGetValue(version, out var v))
                return false;
            v.Published = true;
            return true;
        }
    }

    public bool DeletePackVersion(string org, string name, long version)
    {
        if (!Org(org).Packs.TryGetValue(name, out var pack))
            return false;
        lock (pack.Versions)
            return pack.Versions.Remove(version);
    }

    public bool DeletePack(string org, string name) => Org(org).Packs.TryRemove(name, out _);
}
