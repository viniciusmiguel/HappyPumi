#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using Microsoft.Extensions.Configuration;

namespace HappyPumi.Api.Webhooks;

/// <summary>
/// Fires webhooks on real events: renders the body for the webhook's format, signs it with HMAC, POSTs it,
/// and records the delivery. Reused by every webhook scope (stack/org/env). A failure or SSRF-blocked target
/// is recorded as a delivery (status 0), never thrown, so firing can never fault the triggering operation.
/// </summary>
public interface IWebhookDispatcher
{
    /// <summary>Fires to each active webhook whose filters match the event; returns the recorded deliveries.</summary>
    Task<IReadOnlyList<StoredWebhookDelivery>> FireAsync(
        WebhookScope scope, IEnumerable<WebhookResponse> webhooks, string @event, object payload, CancellationToken ct);

    /// <summary>Sends one already-rendered body to a single webhook (ping/redeliver). Records + returns the delivery.</summary>
    Task<StoredWebhookDelivery> SendAsync(
        WebhookScope scope, WebhookResponse webhook, string @event, string body, CancellationToken ct);
}

/// <summary>HTTP <see cref="IWebhookDispatcher"/> over a typed <see cref="HttpClient"/> (CLAUDE.md owned-seam).</summary>
public sealed class WebhookDispatcher : IWebhookDispatcher
{
    private readonly HttpClient _http;
    private readonly IWebhookDeliveryStore _deliveries;
    private readonly IReadOnlyDictionary<string, IWebhookPayloadFormatter> _formatters;
    private readonly HashSet<string> _blockedHosts;

    public WebhookDispatcher(HttpClient http, IWebhookDeliveryStore deliveries,
        IEnumerable<IWebhookPayloadFormatter> formatters, IConfiguration config)
    {
        _http = http;
        _deliveries = deliveries;
        _formatters = formatters.ToDictionary(f => f.Format, StringComparer.OrdinalIgnoreCase);
        _blockedHosts = new HashSet<string>(
            (config["Webhooks:BlockedHosts"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<StoredWebhookDelivery>> FireAsync(
        WebhookScope scope, IEnumerable<WebhookResponse> webhooks, string @event, object payload, CancellationToken ct)
    {
        var results = new List<StoredWebhookDelivery>();
        foreach (var webhook in webhooks.Where(w => Matches(w, @event)))
            results.Add(await SendAsync(scope, webhook, @event, Render(webhook.Format, @event, payload), ct));
        return results;
    }

    public async Task<StoredWebhookDelivery> SendAsync(
        WebhookScope scope, WebhookResponse webhook, string @event, string body, CancellationToken ct)
    {
        var delivery = NewDelivery(scope, webhook, @event, body);
        if (HostOf(webhook.PayloadUrl) is { } host && _blockedHosts.Contains(host))
            delivery.ResponseBody = $"blocked: host '{host}' is denied by Webhooks:BlockedHosts";
        else
            await PostAsync(webhook, body, delivery, ct);
        return _deliveries.Append(delivery);
    }

    private static bool Matches(WebhookResponse w, string @event)
        => w.Active && (w.Filters is null || w.Filters.Count == 0 || w.Filters.Contains(@event));

    private string Render(string? format, string @event, object payload)
    {
        var formatter = _formatters.GetValueOrDefault(format ?? "raw") ?? _formatters["raw"];
        return formatter.Render(@event, payload);
    }

    private static StoredWebhookDelivery NewDelivery(WebhookScope scope, WebhookResponse webhook, string @event, string body)
        => new()
        {
            Id = Guid.NewGuid().ToString("N"), Scope = scope, WebhookName = webhook.Name,
            Event = @event, RequestBody = body, ResponseStatus = 0, Timestamp = DateTime.UtcNow,
        };

    private async Task PostAsync(WebhookResponse webhook, string body, StoredWebhookDelivery delivery, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, webhook.PayloadUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            if (!string.IsNullOrEmpty(webhook.Secret))
                req.Headers.TryAddWithoutValidation(WebhookSignature.HeaderName, WebhookSignature.Sign(body, webhook.Secret));
            using var res = await _http.SendAsync(req, ct);
            delivery.ResponseStatus = (int)res.StatusCode;
            delivery.ResponseBody = await res.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex) // record transport failures (DNS/refused/timeout) rather than faulting the caller
        {
            delivery.ResponseStatus = 0;
            delivery.ResponseBody = ex.Message;
        }
        finally
        {
            delivery.DurationMs = sw.ElapsedMilliseconds;
        }
    }

    private static string? HostOf(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
}
