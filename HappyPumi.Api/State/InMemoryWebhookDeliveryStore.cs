#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IWebhookDeliveryStore"/> (ADR-0005), keyed by (scope, webhook name).</summary>
public sealed class InMemoryWebhookDeliveryStore : IWebhookDeliveryStore
{
    private readonly ConcurrentDictionary<(string, string, string), List<StoredWebhookDelivery>> _byWebhook = new();

    private static (string, string, string) Key(WebhookScope scope, string webhookName)
        => (scope.Kind, scope.Id, webhookName);

    public StoredWebhookDelivery Append(StoredWebhookDelivery delivery)
    {
        var list = _byWebhook.GetOrAdd(Key(delivery.Scope, delivery.WebhookName), _ => new List<StoredWebhookDelivery>());
        lock (list)
            list.Add(delivery);
        return delivery;
    }

    public IReadOnlyList<StoredWebhookDelivery> List(WebhookScope scope, string webhookName)
    {
        if (!_byWebhook.TryGetValue(Key(scope, webhookName), out var list))
            return new List<StoredWebhookDelivery>();
        lock (list)
            return list.OrderByDescending(d => d.Timestamp).ToList();
    }

    public StoredWebhookDelivery? LatestByEvent(WebhookScope scope, string webhookName, string @event)
        => List(scope, webhookName).FirstOrDefault(d => d.Event == @event);
}
