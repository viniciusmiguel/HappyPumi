#nullable enable

using System.Text.Json;

namespace HappyPumi.Api.Webhooks;

/// <summary>Default formatter: the event payload serialized verbatim as JSON.</summary>
public sealed class RawFormatter : IWebhookPayloadFormatter
{
    public string Format => "raw";

    public string Render(string @event, object payload)
        => JsonSerializer.Serialize(payload);
}

/// <summary>
/// Slack incoming-webhook shape: a top-level <c>text</c> summary plus the raw payload as an attachment so
/// the message renders in a channel.
/// </summary>
public sealed class SlackFormatter : IWebhookPayloadFormatter
{
    public string Format => "slack";

    public string Render(string @event, object payload)
        => JsonSerializer.Serialize(new
        {
            text = $"Pulumi event: {@event}",
            attachments = new[] { new { text = JsonSerializer.Serialize(payload) } },
        });
}

/// <summary>Microsoft Teams MessageCard shape (connector cards expect <c>@type</c>/<c>@context</c>).</summary>
public sealed class MsTeamsFormatter : IWebhookPayloadFormatter
{
    public string Format => "ms_teams";

    public string Render(string @event, object payload)
        => JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["@type"] = "MessageCard",
            ["@context"] = "https://schema.org/extensions",
            ["summary"] = $"Pulumi event: {@event}",
            ["text"] = JsonSerializer.Serialize(payload),
        });
}

/// <summary>
/// Pulumi-deployments format: a structured envelope (<c>kind</c> + <c>payload</c>) used by deployment-driven
/// integrations that key off the event kind.
/// </summary>
public sealed class PulumiDeploymentsFormatter : IWebhookPayloadFormatter
{
    public string Format => "pulumi_deployments";

    public string Render(string @event, object payload)
        => JsonSerializer.Serialize(new { kind = @event, payload });
}
