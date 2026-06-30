#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="ICmkStore"/> (ADR-0005): customer-managed keys keyed by id and scoped by Org,
/// plus their KEK migration history. Creating a key or setting a new default demotes the previous default and
/// appends a migration row.
/// </summary>
public sealed class PostgresCmkStore(HappyPumiDbContext db) : ICmkStore
{
    public StoredCmk Create(string org, string name, string keyType, string? keyArn, string? roleArn)
    {
        Demote(org);
        var row = new CmkRow
        {
            Id = Guid.NewGuid().ToString(), Org = org, Name = name, KeyType = keyType,
            KeyArn = keyArn, RoleArn = roleArn, IsDefault = true, Enabled = true, Created = DateTime.UtcNow,
        };
        db.CustomerManagedKeys.Add(row);
        AddMigration(org);
        db.SaveChanges();
        return Map(row);
    }

    public IReadOnlyList<StoredCmk> List(string org)
        => db.CustomerManagedKeys.AsNoTracking().Where(k => k.Org == org)
            .OrderByDescending(k => k.Created).ToList().Select(Map).ToList();

    public StoredCmk? Get(string org, string id)
    {
        var row = db.CustomerManagedKeys.AsNoTracking().FirstOrDefault(k => k.Org == org && k.Id == id);
        return row is null ? null : Map(row);
    }

    public bool SetDefault(string org, string id)
    {
        var row = db.CustomerManagedKeys.FirstOrDefault(k => k.Org == org && k.Id == id);
        if (row is null)
            return false;
        Demote(org);
        row.IsDefault = true;
        row.Enabled = true;
        AddMigration(org);
        db.SaveChanges();
        return true;
    }

    public bool Disable(string org, string id)
    {
        var row = db.CustomerManagedKeys.FirstOrDefault(k => k.Org == org && k.Id == id);
        if (row is null)
            return false;
        row.Enabled = false;
        row.IsDefault = false;
        db.SaveChanges();
        return true;
    }

    public int DisableAll(string org)
    {
        var rows = db.CustomerManagedKeys.Where(k => k.Org == org).ToList();
        var disabled = rows.Count(k => k.Enabled);
        foreach (var row in rows)
        {
            row.Enabled = false;
            row.IsDefault = false;
        }
        db.SaveChanges();
        return disabled;
    }

    public IReadOnlyList<StoredKeyMigration> ListMigrations(string org)
        => db.KeyMigrations.AsNoTracking().Where(m => m.Org == org)
            .OrderByDescending(m => m.Created).ToList().Select(Map).ToList();

    public int RetryMigrations(string org)
    {
        var failed = db.KeyMigrations.Where(m => m.Org == org && m.State == "failed").ToList();
        foreach (var migration in failed)
            migration.State = "completed";
        db.SaveChanges();
        return failed.Count;
    }

    private void Demote(string org)
    {
        foreach (var row in db.CustomerManagedKeys.Where(k => k.Org == org && k.IsDefault))
            row.IsDefault = false;
    }

    private void AddMigration(string org)
        => db.KeyMigrations.Add(new KeyMigrationRow { Id = Guid.NewGuid().ToString(), Org = org, Created = DateTime.UtcNow });

    private static StoredCmk Map(CmkRow r) => new()
    {
        Id = r.Id, Org = r.Org, Name = r.Name, KeyType = r.KeyType, KeyArn = r.KeyArn,
        RoleArn = r.RoleArn, IsDefault = r.IsDefault, Enabled = r.Enabled, Created = r.Created,
    };

    private static StoredKeyMigration Map(KeyMigrationRow r) => new()
    {
        Id = r.Id, Org = r.Org, State = r.State, Created = r.Created,
    };
}
