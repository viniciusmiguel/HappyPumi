#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IServiceStore"/> (ADR-0005).</summary>
public sealed class PostgresServiceStore(HappyPumiDbContext db) : IServiceStore
{
    public IReadOnlyList<ServiceRow> List(string org)
        => db.Services.AsNoTracking().Where(s => s.Org == org).OrderBy(s => s.Name).ToList();

    public ServiceRow? Create(string org, string name, string displayName, string description)
    {
        if (db.Services.Any(s => s.Org == org && s.Name == name))
            return null;
        var row = new ServiceRow { Org = org, Name = name, DisplayName = displayName, Description = description };
        db.Services.Add(row);
        db.SaveChanges();
        return row;
    }

    public bool Delete(string org, string name)
    {
        var row = db.Services.FirstOrDefault(s => s.Org == org && s.Name == name);
        if (row is null) return false;
        db.Services.Remove(row);
        db.SaveChanges();
        return true;
    }

    public ServiceRow? Get(string org, string name)
        => db.Services.AsNoTracking().FirstOrDefault(s => s.Org == org && s.Name == name);

    public ServiceRow? Update(string org, string name, string? displayName, string? description)
    {
        var row = db.Services.FirstOrDefault(s => s.Org == org && s.Name == name);
        if (row is null) return null;
        if (displayName is not null) row.DisplayName = displayName;
        if (description is not null) row.Description = description;
        db.SaveChanges();
        return row;
    }

    public ServiceRow? AddItems(string org, string name, IEnumerable<string> items)
    {
        var row = db.Services.FirstOrDefault(s => s.Org == org && s.Name == name);
        if (row is null) return null;
        row.Items = row.Items.Union(items).ToList(); // dedupe: adding an existing item is idempotent
        db.SaveChanges();
        return row;
    }

    public ServiceRow? RemoveItem(string org, string name, string item)
    {
        var row = db.Services.FirstOrDefault(s => s.Org == org && s.Name == name);
        if (row is null || !row.Items.Contains(item)) return null;
        row.Items = row.Items.Where(i => i != item).ToList();
        db.SaveChanges();
        return row;
    }
}

/// <summary>PostgreSQL-backed <see cref="ICloudAccountStore"/>.</summary>
public sealed class PostgresCloudAccountStore(HappyPumiDbContext db) : ICloudAccountStore
{
    public IReadOnlyList<CloudAccountRow> List(string org)
        => db.CloudAccounts.AsNoTracking().Where(a => a.Org == org).OrderBy(a => a.Name).ToList();

    public CloudAccountRow? Create(string org, string name, string provider, string description)
    {
        if (db.CloudAccounts.Any(a => a.Org == org && a.Name == name))
            return null;
        var row = new CloudAccountRow { Org = org, Name = name, Provider = provider, Description = description };
        db.CloudAccounts.Add(row);
        db.SaveChanges();
        return row;
    }

    public bool Delete(string org, string name)
    {
        var row = db.CloudAccounts.FirstOrDefault(a => a.Org == org && a.Name == name);
        if (row is null) return false;
        db.CloudAccounts.Remove(row);
        db.SaveChanges();
        return true;
    }
}

/// <summary>PostgreSQL-backed <see cref="IVcsConnectionStore"/> (ADR-0009).</summary>
public sealed class PostgresVcsConnectionStore(HappyPumiDbContext db) : IVcsConnectionStore
{
    public IReadOnlyList<VcsConnectionRow> List(string org)
        => db.VcsConnections.AsNoTracking().Where(c => c.Org == org).OrderBy(c => c.Name).ToList();

    public VcsConnectionRow? Create(string org, string name, string kind)
    {
        if (db.VcsConnections.Any(c => c.Org == org && c.Name == name))
            return null;
        var row = new VcsConnectionRow { Org = org, Name = name, Kind = kind };
        db.VcsConnections.Add(row);
        db.SaveChanges();
        return row;
    }

    public bool Delete(string org, string name)
    {
        var row = db.VcsConnections.FirstOrDefault(c => c.Org == org && c.Name == name);
        if (row is null) return false;
        db.VcsConnections.Remove(row);
        db.SaveChanges();
        return true;
    }
}

/// <summary>PostgreSQL-backed <see cref="IOidcIssuerStore"/>.</summary>
public sealed class PostgresOidcIssuerStore(HappyPumiDbContext db) : IOidcIssuerStore
{
    public IReadOnlyList<OidcIssuerRow> List(string org)
        => db.OidcIssuers.AsNoTracking().Where(i => i.Org == org).OrderBy(i => i.Name).ToList();

    public OidcIssuerRow? Get(string org, string id)
        => db.OidcIssuers.AsNoTracking().FirstOrDefault(i => i.Org == org && i.Id == id);

    public OidcIssuerRow? Create(string org, string name, string url)
        => Create(org, name, url, null, null);

    public OidcIssuerRow? Create(string org, string name, string url, List<string>? thumbprints, long? maxExpiration)
    {
        if (db.OidcIssuers.Any(i => i.Org == org && i.Name == name))
            return null;
        var row = new OidcIssuerRow
        {
            Org = org, Name = name, Url = url, Id = Guid.NewGuid().ToString(),
            Thumbprints = thumbprints ?? new(), MaxExpiration = maxExpiration,
        };
        db.OidcIssuers.Add(row);
        db.SaveChanges();
        return row;
    }

    public OidcIssuerRow? Update(string org, string id, string? name, long? maxExpiration, List<string>? thumbprints)
    {
        var row = db.OidcIssuers.FirstOrDefault(i => i.Org == org && i.Id == id);
        if (row is null) return null;
        // Name is part of the (Org,Name) primary key, so a rename is a delete+insert keeping the same Id.
        if (name is not null && name != row.Name)
            return Recreate(row, name, maxExpiration, thumbprints);
        if (maxExpiration is not null) row.MaxExpiration = maxExpiration;
        if (thumbprints is not null) row.Thumbprints = thumbprints;
        row.Modified = DateTime.UtcNow;
        db.SaveChanges();
        return row;
    }

    public OidcIssuerRow? SetThumbprints(string org, string id, IReadOnlyList<string> thumbprints)
    {
        var row = db.OidcIssuers.FirstOrDefault(i => i.Org == org && i.Id == id);
        if (row is null) return null;
        row.Thumbprints = thumbprints.ToList();
        row.Modified = DateTime.UtcNow;
        db.SaveChanges();
        return row;
    }

    public bool Delete(string org, string name)
    {
        var row = db.OidcIssuers.FirstOrDefault(i => i.Org == org && i.Name == name);
        if (row is null) return false;
        db.OidcIssuers.Remove(row);
        db.SaveChanges();
        return true;
    }

    public bool DeleteById(string org, string id)
    {
        var row = db.OidcIssuers.FirstOrDefault(i => i.Org == org && i.Id == id);
        if (row is null) return false;
        db.OidcIssuers.Remove(row);
        db.SaveChanges();
        return true;
    }

    private OidcIssuerRow Recreate(OidcIssuerRow old, string newName, long? maxExpiration, List<string>? thumbprints)
    {
        var replacement = new OidcIssuerRow
        {
            Org = old.Org, Name = newName, Url = old.Url, Id = old.Id, Created = old.Created,
            Thumbprints = thumbprints ?? old.Thumbprints, MaxExpiration = maxExpiration ?? old.MaxExpiration,
            Modified = DateTime.UtcNow, LastUsed = old.LastUsed,
        };
        db.OidcIssuers.Remove(old);
        db.OidcIssuers.Add(replacement);
        db.SaveChanges();
        return replacement;
    }
}

/// <summary>PostgreSQL-backed <see cref="IApprovalRuleStore"/>.</summary>
public sealed class PostgresApprovalRuleStore(HappyPumiDbContext db) : IApprovalRuleStore
{
    public IReadOnlyList<ApprovalRuleRow> List(string org)
        => db.ApprovalRules.AsNoTracking().Where(r => r.Org == org).OrderBy(r => r.Name).ToList();

    public ApprovalRuleRow? Create(string org, string name, string stackPattern, int requiredApprovals)
    {
        if (db.ApprovalRules.Any(r => r.Org == org && r.Name == name))
            return null;
        var row = new ApprovalRuleRow
        {
            Org = org, Name = name, StackPattern = stackPattern, RequiredApprovals = requiredApprovals,
        };
        db.ApprovalRules.Add(row);
        db.SaveChanges();
        return row;
    }

    public bool Delete(string org, string name)
    {
        var row = db.ApprovalRules.FirstOrDefault(r => r.Org == org && r.Name == name);
        if (row is null) return false;
        db.ApprovalRules.Remove(row);
        db.SaveChanges();
        return true;
    }
}
