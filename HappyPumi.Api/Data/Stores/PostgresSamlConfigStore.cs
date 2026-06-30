#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="ISamlConfigStore"/> (ADR-0005): one SAML configuration per org, keyed by Org.
/// The admin list is persisted as jsonb. <see cref="Upsert"/> creates or replaces the org's config.
/// </summary>
public sealed class PostgresSamlConfigStore(HappyPumiDbContext db) : ISamlConfigStore
{
    public StoredSamlConfig? Get(string org)
    {
        var row = db.SamlConfigs.AsNoTracking().FirstOrDefault(c => c.Org == org);
        return row is null ? null : Map(row);
    }

    public StoredSamlConfig Upsert(StoredSamlConfig config)
    {
        var row = db.SamlConfigs.FirstOrDefault(c => c.Org == config.Org);
        if (row is null)
        {
            row = new SamlConfigRow { Org = config.Org };
            db.SamlConfigs.Add(row);
        }
        Apply(config, row);
        db.SaveChanges();
        return Map(row);
    }

    public IReadOnlyList<string> ListAdmins(string org)
        => Get(org)?.Admins ?? new List<string>();

    public bool AddAdmin(string org, string userLogin)
    {
        var row = db.SamlConfigs.FirstOrDefault(c => c.Org == org);
        if (row is null)
            return false;
        if (!row.Admins.Contains(userLogin))
            row.Admins = [.. row.Admins, userLogin]; // reassign so the jsonb comparer sees the change
        db.SaveChanges();
        return true;
    }

    private static void Apply(StoredSamlConfig src, SamlConfigRow row)
    {
        row.IdpMetadataXml = src.IdpMetadataXml;
        row.EntityId = src.EntityId;
        row.SsoUrl = src.SsoUrl;
        row.Certificate = src.Certificate;
        row.NameIdFormat = src.NameIdFormat;
        row.ValidUntil = src.ValidUntil;
        row.ValidationError = src.ValidationError;
        row.Enabled = src.Enabled;
        row.Admins = [.. src.Admins];
    }

    private static StoredSamlConfig Map(SamlConfigRow r) => new()
    {
        Org = r.Org, IdpMetadataXml = r.IdpMetadataXml, EntityId = r.EntityId, SsoUrl = r.SsoUrl,
        Certificate = r.Certificate, NameIdFormat = r.NameIdFormat, ValidUntil = r.ValidUntil,
        ValidationError = r.ValidationError, Enabled = r.Enabled, Admins = [.. r.Admins],
    };
}
