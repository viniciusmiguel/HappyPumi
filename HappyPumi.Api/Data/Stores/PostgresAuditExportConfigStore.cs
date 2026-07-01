#nullable enable

using System;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="IAuditExportConfigStore"/> (ADR-0005): one config row per org, keyed by Org.
/// <see cref="Get"/> returns a fresh disabled default (not persisted) when the row is absent;
/// <see cref="Upsert"/> creates the row on first write; <see cref="Delete"/> removes it.
/// </summary>
public sealed class PostgresAuditExportConfigStore(HappyPumiDbContext db) : IAuditExportConfigStore
{
    public StoredAuditExportConfig Get(string org)
    {
        var row = db.AuditExportConfigs.AsNoTracking().FirstOrDefault(c => c.Org == org);
        return row is null ? new StoredAuditExportConfig { Org = org } : Map(row);
    }

    public StoredAuditExportConfig Upsert(string org, Action<StoredAuditExportConfig> mutate)
    {
        var row = db.AuditExportConfigs.FirstOrDefault(c => c.Org == org);
        if (row is null)
        {
            row = new AuditExportConfigRow { Org = org, Created = DateTime.UtcNow };
            db.AuditExportConfigs.Add(row);
        }
        var config = Map(row);
        mutate(config);
        Apply(config, row);
        db.SaveChanges();
        return Map(row);
    }

    public bool Delete(string org)
    {
        var row = db.AuditExportConfigs.FirstOrDefault(c => c.Org == org);
        if (row is null)
            return false;
        db.AuditExportConfigs.Remove(row);
        db.SaveChanges();
        return true;
    }

    private static void Apply(StoredAuditExportConfig src, AuditExportConfigRow row)
    {
        row.Enabled = src.Enabled;
        row.IamRoleArn = src.IamRoleArn;
        row.S3BucketName = src.S3BucketName;
        row.S3PathPrefix = src.S3PathPrefix;
        row.LastResultMessage = src.LastResultMessage;
        row.LastResultTimestamp = src.LastResultTimestamp;
    }

    private static StoredAuditExportConfig Map(AuditExportConfigRow r) => new()
    {
        Org = r.Org,
        Enabled = r.Enabled,
        IamRoleArn = r.IamRoleArn,
        S3BucketName = r.S3BucketName,
        S3PathPrefix = r.S3PathPrefix,
        LastResultMessage = r.LastResultMessage,
        LastResultTimestamp = r.LastResultTimestamp,
        Created = r.Created,
    };
}
