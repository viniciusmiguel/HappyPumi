using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using HappyPumi.Api.Webhooks;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>In-memory <see cref="IWebhookDeliveryLog"/> for unit tests (the real impl is Postgres-backed).</summary>
public sealed class FakeWebhookDeliveryLog : IWebhookDeliveryLog
{
    private readonly Dictionary<string, List<WebhookDelivery>> _deliveries = new();

    private static string Key(EnvCoordinates e, string hook) => $"{e.Org}/{e.Project}/{e.Name}/{hook}";

    public void Record(EnvCoordinates environment, string hookName, WebhookDelivery delivery)
    {
        var key = Key(environment, hookName);
        (_deliveries.TryGetValue(key, out var list) ? list : _deliveries[key] = new()).Insert(0, delivery);
    }

    public IReadOnlyList<WebhookDelivery> List(EnvCoordinates environment, string hookName)
        => _deliveries.TryGetValue(Key(environment, hookName), out var list) ? list.ToArray() : new WebhookDelivery[0];

    public WebhookDelivery? Get(EnvCoordinates environment, string hookName, string deliveryId)
        => List(environment, hookName).FirstOrDefault(d => d.Id == deliveryId);
}
