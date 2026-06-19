#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Webhooks;

/// <summary>The outcome of delivering a webhook payload (HTTP status, body, headers, elapsed time).</summary>
public readonly record struct WebhookSendResult(long ResponseCode, string ResponseBody, string ResponseHeaders, long DurationMs);

/// <summary>
/// Owned seam over the HTTP delivery of a webhook (CLAUDE.md: wrap third-party I/O). Lets the delivery
/// service be unit-tested with a named fake instead of making real HTTP calls.
/// </summary>
public interface IWebhookSender
{
    /// <summary>POSTs <paramref name="payload"/> to <paramref name="url"/>, signing it when a secret is set.</summary>
    Task<WebhookSendResult> SendAsync(string url, string payload, string? secret, CancellationToken ct);
}
