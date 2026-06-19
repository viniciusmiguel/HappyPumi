#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Webhooks;

/// <summary>
/// Delivers webhooks and records the attempts: a <em>ping</em> sends a synthetic test event; a <em>redeliver</em>
/// re-sends a previously recorded delivery's payload. Each attempt is logged and returned as a
/// <see cref="WebhookDelivery"/>.
/// </summary>
public sealed class WebhookDeliveryService(IEnvironmentWebhookStore webhooks, IWebhookSender sender, IWebhookDeliveryLog log)
{
    /// <summary>Sends a test "ping" to the webhook. Null when the webhook does not exist.</summary>
    public async Task<WebhookDelivery?> PingAsync(EnvCoordinates env, string hookName, CancellationToken ct)
    {
        var webhook = webhooks.Get(env, hookName);
        if (webhook is null)
            return null;
        var payload = JsonSerializer.Serialize(new { kind = "ping", environment = $"{env.Org}/{env.Project}/{env.Name}", timestamp = Now() });
        return await Deliver(env, hookName, webhook, "ping", payload, ct);
    }

    /// <summary>Re-sends a recorded delivery's payload. Null when the webhook or the prior delivery is missing.</summary>
    public async Task<WebhookDelivery?> RedeliverAsync(EnvCoordinates env, string hookName, string deliveryId, CancellationToken ct)
    {
        var webhook = webhooks.Get(env, hookName);
        var prior = log.Get(env, hookName, deliveryId);
        if (webhook is null || prior is null)
            return null;
        return await Deliver(env, hookName, webhook, prior.Kind, prior.Payload, ct);
    }

    private async Task<WebhookDelivery> Deliver(EnvCoordinates env, string hookName, StoredWebhook webhook,
        string kind, string payload, CancellationToken ct)
    {
        var result = await sender.SendAsync(webhook.PayloadUrl, payload, webhook.Secret, ct);
        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = kind,
            Payload = payload,
            RequestUrl = webhook.PayloadUrl,
            RequestHeaders = "Content-Type: application/json",
            ResponseCode = result.ResponseCode,
            ResponseBody = result.ResponseBody,
            ResponseHeaders = result.ResponseHeaders,
            Duration = result.DurationMs,
            Timestamp = Now(),
        };
        log.Record(env, hookName, delivery);
        return delivery;
    }

    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
