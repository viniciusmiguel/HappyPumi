#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL <see cref="IEnvironmentWebhookStore"/>: environment webhook definitions.</summary>
public sealed class PostgresEnvironmentWebhookStore(HappyPumiDbContext db) : IEnvironmentWebhookStore
{
    public IReadOnlyList<StoredWebhook> List(EnvCoordinates e)
        => db.EnvironmentWebhooks.AsNoTracking()
            .Where(w => w.Org == e.Org && w.Project == e.Project && w.EnvName == e.Name)
            .ToList().OrderBy(w => w.Name).Select(Map).ToList();

    public StoredWebhook? Get(EnvCoordinates e, string name)
    {
        var row = Row(e, name);
        return row is null ? null : Map(row);
    }

    public StoredWebhook? Create(EnvCoordinates e, StoredWebhook webhook)
    {
        if (Row(e, webhook.Name) is not null)
            return null;
        var row = new EnvironmentWebhookRow
        {
            Id = Guid.NewGuid().ToString(), Org = e.Org, Project = e.Project, EnvName = e.Name, Name = webhook.Name,
            Created = DateTime.UtcNow,
        };
        Apply(row, webhook);
        db.EnvironmentWebhooks.Add(row);
        db.SaveChanges();
        return Map(row);
    }

    public StoredWebhook? Update(EnvCoordinates e, string name, StoredWebhook webhook)
    {
        var row = Row(e, name);
        if (row is null)
            return null;
        Apply(row, webhook);
        db.SaveChanges();
        return Map(row);
    }

    public bool Delete(EnvCoordinates e, string name)
    {
        var row = Row(e, name);
        if (row is null)
            return false;
        db.EnvironmentWebhooks.Remove(row);
        db.SaveChanges();
        return true;
    }

    private EnvironmentWebhookRow? Row(EnvCoordinates e, string name)
        => db.EnvironmentWebhooks.FirstOrDefault(w => w.Org == e.Org && w.Project == e.Project && w.EnvName == e.Name && w.Name == name);

    // Copy editable fields; the secret is only replaced when a new one is supplied (so updates can omit it).
    private static void Apply(EnvironmentWebhookRow row, StoredWebhook webhook)
    {
        row.DisplayName = webhook.DisplayName;
        row.PayloadUrl = webhook.PayloadUrl;
        row.Active = webhook.Active;
        row.Format = webhook.Format;
        if (webhook.Secret is not null)
            row.Secret = webhook.Secret;
        row.Filters = new List<string>(webhook.Filters); // new instance so EF tracks the jsonb change
        row.Groups = new List<string>(webhook.Groups);
    }

    private static StoredWebhook Map(EnvironmentWebhookRow r) => new()
    {
        Name = r.Name, DisplayName = r.DisplayName, PayloadUrl = r.PayloadUrl, Active = r.Active,
        Format = r.Format, Secret = r.Secret, Filters = r.Filters, Groups = r.Groups, Created = r.Created,
    };
}
