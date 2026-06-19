#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Webhooks;

/// <summary>
/// Persistence seam for webhook delivery attempts per (environment, webhook). Backed by PostgreSQL
/// (see <c>PostgresWebhookDeliveryLog</c>).
/// </summary>
public interface IWebhookDeliveryLog
{
    void Record(EnvCoordinates environment, string hookName, WebhookDelivery delivery);
    IReadOnlyList<WebhookDelivery> List(EnvCoordinates environment, string hookName);
    WebhookDelivery? Get(EnvCoordinates environment, string hookName, string deliveryId);
}
