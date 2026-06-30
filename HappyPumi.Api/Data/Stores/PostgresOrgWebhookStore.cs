#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL <see cref="IOrgWebhookStore"/>: organization webhook definitions, one per (org, name).</summary>
public sealed class PostgresOrgWebhookStore(HappyPumiDbContext db) : IOrgWebhookStore
{
    public IReadOnlyList<WebhookResponse> List(string org)
        => db.OrgWebhooks.AsNoTracking().Where(w => w.Org == org)
            .ToList().OrderBy(w => w.Name).Select(Map).ToList();

    public WebhookResponse? Get(string org, string name)
    {
        var row = Row(org, name);
        return row is null ? null : Map(row);
    }

    public WebhookResponse? Create(string org, WebhookResponse webhook)
    {
        if (Row(org, webhook.Name) is not null)
            return null;
        var row = new OrgWebhookRow
        {
            Id = Guid.NewGuid().ToString(), Org = org, Name = webhook.Name, Created = DateTime.UtcNow,
        };
        Apply(row, webhook);
        db.OrgWebhooks.Add(row);
        db.SaveChanges();
        return Map(row);
    }

    public WebhookResponse? Update(string org, string name, Webhook patch)
    {
        var row = Row(org, name);
        if (row is null)
            return null;
        var view = Map(row);
        StackWebhookMapper.ApplyPatch(view, patch);
        Apply(row, view);
        db.SaveChanges();
        return Map(row);
    }

    public bool Delete(string org, string name)
    {
        var row = Row(org, name);
        if (row is null)
            return false;
        db.OrgWebhooks.Remove(row);
        db.SaveChanges();
        return true;
    }

    private OrgWebhookRow? Row(string org, string name)
        => db.OrgWebhooks.FirstOrDefault(w => w.Org == org && w.Name == name);

    // Copy editable fields; the secret is only replaced when a new one is supplied (so updates can omit it).
    private static void Apply(OrgWebhookRow row, WebhookResponse w)
    {
        row.DisplayName = w.DisplayName;
        row.PayloadUrl = w.PayloadUrl;
        row.Active = w.Active;
        row.Format = w.Format;
        if (!string.IsNullOrEmpty(w.Secret))
            row.Secret = w.Secret;
        row.Filters = w.Filters is null ? null : new List<string>(w.Filters); // new instance so EF tracks the jsonb change
        row.Groups = w.Groups is null ? null : new List<string>(w.Groups);
    }

    private static WebhookResponse Map(OrgWebhookRow r) => new()
    {
        Name = r.Name, DisplayName = r.DisplayName, PayloadUrl = r.PayloadUrl, Active = r.Active,
        Format = r.Format, Filters = r.Filters, Groups = r.Groups, Secret = r.Secret,
        OrganizationName = r.Org, HasSecret = !string.IsNullOrEmpty(r.Secret), SecretCiphertext = "",
    };
}
