using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Webhooks;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IWebhookSender"/>: records sends, returns a canned result.</summary>
public sealed class FakeWebhookSender : IWebhookSender
{
    public WebhookSendResult Next { get; set; } = new(200, "ok", "", 5);
    public List<(string Url, string Payload, string? Secret)> Sends { get; } = new();

    public Task<WebhookSendResult> SendAsync(string url, string payload, string? secret, CancellationToken ct)
    {
        Sends.Add((url, payload, secret));
        return Task.FromResult(Next);
    }
}
