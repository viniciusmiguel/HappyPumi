#nullable enable

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

    public OidcIssuerRow? Create(string org, string name, string url)
    {
        if (db.OidcIssuers.Any(i => i.Org == org && i.Name == name))
            return null;
        var row = new OidcIssuerRow { Org = org, Name = name, Url = url };
        db.OidcIssuers.Add(row);
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
