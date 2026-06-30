#nullable enable

namespace HappyPumi.Api.Webhooks;

/// <summary>
/// Renders the outbound HTTP body for one webhook <c>format</c>. The dispatcher selects the formatter whose
/// <see cref="Format"/> matches the webhook's configured format (defaulting to <c>raw</c>), keeping the
/// per-format shaping (Slack, MS Teams, …) out of the dispatcher itself.
/// </summary>
public interface IWebhookPayloadFormatter
{
    /// <summary>The webhook <c>format</c> this formatter handles: <c>raw</c>|<c>slack</c>|<c>ms_teams</c>|<c>pulumi_deployments</c>.</summary>
    string Format { get; }

    /// <summary>Renders the JSON body sent to the payload URL for <paramref name="event"/> + <paramref name="payload"/>.</summary>
    string Render(string @event, object payload);
}
