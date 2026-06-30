#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IVcsIntegrationStore"/> (ADR-0005 / ADR-0009).</summary>
public sealed class PostgresVcsIntegrationStore(HappyPumiDbContext db) : IVcsIntegrationStore
{
    public StoredVcsIntegration Create(StoredVcsIntegration integration)
    {
        var row = new VcsIntegrationRow
        {
            Id = Guid.NewGuid().ToString(), Org = integration.Org, Kind = integration.Kind,
            Name = integration.Name, BaseUrl = integration.BaseUrl, AccountName = integration.AccountName,
            AccountId = integration.AccountId, AvatarUrl = integration.AvatarUrl,
            AzureProject = integration.AzureProject, Credential = integration.Credential,
            Settings = integration.Settings,
            Created = integration.Created, CreatedBy = integration.CreatedBy,
        };
        db.VcsIntegrations.Add(row);
        db.SaveChanges();
        return Map(row);
    }

    public IReadOnlyList<StoredVcsIntegration> List(string org, string? kind = null)
        => db.VcsIntegrations.Where(i => i.Org == org && (kind == null || i.Kind == kind))
            .OrderBy(i => i.Created).ToList()
            .Select(Map).ToList();

    public StoredVcsIntegration? Get(string org, string id)
    {
        var row = Row(org, id);
        return row is null ? null : Map(row);
    }

    public StoredVcsIntegration? UpdateSettings(string org, string id, VcsIntegrationSettings settings)
    {
        var row = Row(org, id);
        if (row is null)
            return null;
        row.Settings = settings;
        db.SaveChanges();
        return Map(row);
    }

    public StoredVcsIntegration? SetCredential(string org, string id, string credential)
    {
        var row = Row(org, id);
        if (row is null)
            return null;
        row.Credential = credential;
        db.SaveChanges();
        return Map(row);
    }

    public bool Delete(string org, string id)
    {
        var row = Row(org, id);
        if (row is null)
            return false;
        db.VcsIntegrations.Remove(row);
        db.SaveChanges();
        return true;
    }

    private VcsIntegrationRow? Row(string org, string id)
        => db.VcsIntegrations.FirstOrDefault(i => i.Org == org && i.Id == id);

    private static StoredVcsIntegration Map(VcsIntegrationRow r) => new()
    {
        Id = r.Id, Org = r.Org, Kind = r.Kind, Name = r.Name, BaseUrl = r.BaseUrl,
        AccountName = r.AccountName, AccountId = r.AccountId, AvatarUrl = r.AvatarUrl,
        AzureProject = r.AzureProject, Credential = r.Credential, Settings = r.Settings,
        Created = r.Created, CreatedBy = r.CreatedBy,
    };
}
