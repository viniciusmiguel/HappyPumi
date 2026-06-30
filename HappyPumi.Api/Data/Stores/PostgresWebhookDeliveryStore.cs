#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL <see cref="IWebhookDeliveryStore"/>: delivery history per (scope, webhook), newest first.</summary>
public sealed class PostgresWebhookDeliveryStore(HappyPumiDbContext db) : IWebhookDeliveryStore
{
    public StoredWebhookDelivery Append(StoredWebhookDelivery delivery)
    {
        db.WebhookDeliveries.Add(ToRow(delivery));
        db.SaveChanges();
        return delivery;
    }

    public IReadOnlyList<StoredWebhookDelivery> List(WebhookScope scope, string webhookName)
        => db.WebhookDeliveries.AsNoTracking()
            .Where(r => r.ScopeKind == scope.Kind && r.ScopeId == scope.Id && r.WebhookName == webhookName)
            .ToList().OrderByDescending(r => r.Timestamp).Select(ToDomain).ToList();

    public StoredWebhookDelivery? LatestByEvent(WebhookScope scope, string webhookName, string @event)
        => List(scope, webhookName).FirstOrDefault(d => d.Event == @event);

    private static WebhookDeliveryRow ToRow(StoredWebhookDelivery d) => new()
    {
        Id = d.Id, ScopeKind = d.Scope.Kind, ScopeId = d.Scope.Id, WebhookName = d.WebhookName,
        Event = d.Event, RequestBody = d.RequestBody, ResponseStatus = d.ResponseStatus,
        ResponseBody = d.ResponseBody, DurationMs = d.DurationMs, Timestamp = d.Timestamp,
    };

    private static StoredWebhookDelivery ToDomain(WebhookDeliveryRow r) => new()
    {
        Id = r.Id, Scope = new WebhookScope(r.ScopeKind, r.ScopeId), WebhookName = r.WebhookName,
        Event = r.Event, RequestBody = r.RequestBody, ResponseStatus = r.ResponseStatus,
        ResponseBody = r.ResponseBody, DurationMs = r.DurationMs, Timestamp = r.Timestamp,
    };
}
