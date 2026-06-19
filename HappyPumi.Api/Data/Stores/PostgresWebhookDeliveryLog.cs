#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using HappyPumi.Api.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL <see cref="IWebhookDeliveryLog"/>: delivery attempts per (env, webhook), newest first.</summary>
public sealed class PostgresWebhookDeliveryLog(HappyPumiDbContext db) : IWebhookDeliveryLog
{
    public void Record(EnvCoordinates e, string hookName, WebhookDelivery delivery)
    {
        db.EnvironmentWebhookDeliveries.Add(new EnvironmentWebhookDeliveryRow
        {
            Id = delivery.Id, Org = e.Org, Project = e.Project, Name = e.Name, HookName = hookName,
            Created = DateTime.UtcNow, Delivery = delivery,
        });
        db.SaveChanges();
    }

    public IReadOnlyList<WebhookDelivery> List(EnvCoordinates e, string hookName)
        => db.EnvironmentWebhookDeliveries.AsNoTracking()
            .Where(r => r.Org == e.Org && r.Project == e.Project && r.Name == e.Name && r.HookName == hookName)
            .ToList().OrderByDescending(r => r.Created).Select(r => r.Delivery).ToList();

    public WebhookDelivery? Get(EnvCoordinates e, string hookName, string deliveryId)
        => db.EnvironmentWebhookDeliveries.AsNoTracking()
            .FirstOrDefault(r => r.Id == deliveryId && r.Org == e.Org && r.Project == e.Project && r.Name == e.Name && r.HookName == hookName)
            ?.Delivery;
}
