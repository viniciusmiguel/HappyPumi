#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Webhooks;

/// <summary>
/// Real <see cref="IWebhookSender"/>: POSTs the JSON payload over HTTP, adding an HMAC-SHA256 signature header
/// when the webhook has a secret. A transport failure is captured as response code 0 (not thrown), so a
/// delivery is always recorded. A short timeout keeps a slow/unreachable endpoint from blocking the request.
/// </summary>
public sealed class WebhookSender : IWebhookSender
{
    private const string SignatureHeader = "X-Pulumi-Webhook-Signature";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public async Task<WebhookSendResult> SendAsync(string url, string payload, string? secret, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            if (!string.IsNullOrEmpty(secret))
                request.Headers.TryAddWithoutValidation(SignatureHeader, Sign(payload, secret));

            using var response = await Http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            return new WebhookSendResult((long)response.StatusCode, body, HeaderSummary(response), stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new WebhookSendResult(0, ex.Message, "", stopwatch.ElapsedMilliseconds);
        }
    }

    private static string Sign(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return "sha256=" + Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static string HeaderSummary(HttpResponseMessage response)
        => string.Join("\n", response.Headers.Select(h => $"{h.Key}: {string.Join(",", h.Value)}"));
}
