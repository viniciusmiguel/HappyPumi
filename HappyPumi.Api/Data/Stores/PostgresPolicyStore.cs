#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IPolicyStore"/> (ADR-0005).</summary>
public sealed class PostgresPolicyStore(HappyPumiDbContext db) : IPolicyStore
{
    public IReadOnlyCollection<StoredPolicyGroup> ListGroups(string org)
        => db.PolicyGroups.AsNoTracking().Where(g => g.Org == org).ToList().Select(ToGroup).ToList();

    public StoredPolicyGroup? GetGroup(string org, string name)
    {
        var row = db.PolicyGroups.AsNoTracking().FirstOrDefault(g => g.Org == org && g.Name == name);
        return row is null ? null : ToGroup(row);
    }

    public StoredPolicyGroup? NewGroup(string org, string name)
    {
        if (db.PolicyGroups.Any(g => g.Org == org && g.Name == name))
            return null;
        db.PolicyGroups.Add(new PolicyGroupRow { Org = org, Name = name });
        db.SaveChanges();
        return new StoredPolicyGroup { Name = name };
    }

    public bool RenameGroup(string org, string name, string newName)
    {
        var src = db.PolicyGroups.FirstOrDefault(g => g.Org == org && g.Name == name);
        if (src is null || db.PolicyGroups.Any(g => g.Org == org && g.Name == newName))
            return false;
        db.PolicyGroups.Add(new PolicyGroupRow
        {
            Org = org, Name = newName, IsOrgDefault = src.IsOrgDefault,
            Stacks = new List<string>(src.Stacks), AppliedPolicyPacks = new List<string>(src.AppliedPolicyPacks),
        });
        db.PolicyGroups.Remove(src);
        db.SaveChanges();
        return true;
    }

    public bool DeleteGroup(string org, string name)
    {
        var row = db.PolicyGroups.FirstOrDefault(g => g.Org == org && g.Name == name);
        if (row is null)
            return false;
        db.PolicyGroups.Remove(row);
        db.SaveChanges();
        return true;
    }

    public bool AddPackToGroup(string org, string group, string packName)
    {
        var row = db.PolicyGroups.FirstOrDefault(g => g.Org == org && g.Name == group);
        if (row is null)
        {
            row = new PolicyGroupRow
            {
                Org = org, Name = group, IsOrgDefault = group == "default-policy-group",
                AppliedPolicyPacks = new List<string> { packName },
            };
            db.PolicyGroups.Add(row);
        }
        else if (!row.AppliedPolicyPacks.Contains(packName))
        {
            row.AppliedPolicyPacks = new List<string>(row.AppliedPolicyPacks) { packName };
        }
        db.SaveChanges();
        return true;
    }

    public bool RemovePackFromGroup(string org, string group, string packName)
    {
        var row = db.PolicyGroups.FirstOrDefault(g => g.Org == org && g.Name == group);
        if (row is null || !row.AppliedPolicyPacks.Contains(packName))
            return false;
        row.AppliedPolicyPacks = row.AppliedPolicyPacks.Where(p => p != packName).ToList();
        db.SaveChanges();
        return true;
    }

    public IReadOnlyCollection<StoredPolicyPack> ListPacks(string org)
        => db.PolicyPackVersions.AsNoTracking().Where(p => p.Org == org).ToList()
            .GroupBy(p => p.Name).Select(ToPack).ToList();

    public long CreatePackVersion(string org, string name, string displayName, List<AppPolicy>? policies, string? versionTag = null)
    {
        var existing = db.PolicyPackVersions.Where(p => p.Org == org && p.Name == name).Select(p => p.Version).ToList();
        var version = existing.Count == 0 ? 1 : existing.Max() + 1;
        db.PolicyPackVersions.Add(new PolicyPackVersionRow
        {
            Org = org, Name = name, Version = version, DisplayName = displayName, Policies = policies, VersionTag = versionTag,
        });
        db.SaveChanges();
        return version;
    }

    public StoredPolicyPack? GetPack(string org, string name)
    {
        var rows = db.PolicyPackVersions.AsNoTracking().Where(p => p.Org == org && p.Name == name).ToList();
        return rows.Count == 0 ? null : ToPack(rows.GroupBy(p => p.Name).First());
    }

    public bool CompletePack(string org, string name, long version)
    {
        var row = PackRow(org, name, version);
        if (row is null)
            return false;
        row.Published = true;
        db.SaveChanges();
        return true;
    }

    public bool DeletePackVersion(string org, string name, long version)
    {
        var row = PackRow(org, name, version);
        if (row is null)
            return false;
        db.PolicyPackVersions.Remove(row);
        db.SaveChanges();
        return true;
    }

    public bool DeletePack(string org, string name)
    {
        var rows = db.PolicyPackVersions.Where(p => p.Org == org && p.Name == name).ToList();
        if (rows.Count == 0)
            return false;
        db.PolicyPackVersions.RemoveRange(rows);
        db.SaveChanges();
        return true;
    }

    private PolicyPackVersionRow? PackRow(string org, string name, long version)
        => db.PolicyPackVersions.FirstOrDefault(p => p.Org == org && p.Name == name && p.Version == version);

    private static StoredPolicyGroup ToGroup(PolicyGroupRow r)
    {
        var g = new StoredPolicyGroup { Name = r.Name, IsOrgDefault = r.IsOrgDefault };
        g.Stacks.AddRange(r.Stacks);
        g.AppliedPolicyPacks.AddRange(r.AppliedPolicyPacks);
        return g;
    }

    private static StoredPolicyPack ToPack(IGrouping<string, PolicyPackVersionRow> g)
    {
        var pack = new StoredPolicyPack { Name = g.Key, DisplayName = g.First().DisplayName };
        foreach (var v in g)
            pack.Versions[v.Version] = new StoredPolicyPackVersion
            {
                Version = v.Version, VersionTag = v.VersionTag, Published = v.Published, Policies = v.Policies,
            };
        return pack;
    }
}
