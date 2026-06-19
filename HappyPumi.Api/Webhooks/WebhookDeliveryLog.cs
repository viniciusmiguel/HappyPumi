#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Webhooks;

/// <summary>Records and retrieves webhook delivery attempts per (environment, webhook).</summary>
public interface IWebhookDeliveryLog
{
    void Record(EnvCoordinates environment, string hookName, WebhookDelivery delivery);
    IReadOnlyList<WebhookDelivery> List(EnvCoordinates environment, string hookName);
    WebhookDelivery? Get(EnvCoordinates environment, string hookName, string deliveryId);
}

/// <summary>
/// In-memory <see cref="IWebhookDeliveryLog"/>. Deliveries are operational telemetry (like rotation history),
/// kept in-process for now; a durable store is a follow-up.
/// </summary>
public sealed class WebhookDeliveryLog : IWebhookDeliveryLog
{
    private readonly ConcurrentDictionary<string, List<WebhookDelivery>> _deliveries = new();

    private static string Key(EnvCoordinates e, string hook) => $"{e.Org}/{e.Project}/{e.Name}/{hook}";

    public void Record(EnvCoordinates environment, string hookName, WebhookDelivery delivery)
    {
        var list = _deliveries.GetOrAdd(Key(environment, hookName), _ => new List<WebhookDelivery>());
        lock (list)
            list.Insert(0, delivery); // newest first
    }

    public IReadOnlyList<WebhookDelivery> List(EnvCoordinates environment, string hookName)
    {
        if (!_deliveries.TryGetValue(Key(environment, hookName), out var list))
            return new List<WebhookDelivery>();
        lock (list)
            return list.ToArray();
    }

    public WebhookDelivery? Get(EnvCoordinates environment, string hookName, string deliveryId)
        => List(environment, hookName).FirstOrDefault(d => d.Id == deliveryId);
}
