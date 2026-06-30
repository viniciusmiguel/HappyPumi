#nullable enable

using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Maps stack webhooks for the wire: applies a PATCH body to a stored webhook, and produces a client-safe
/// view that reports only the <em>presence</em> of a secret (never echoes it — the dispatcher keeps the
/// stored secret for signing).
/// </summary>
public static class StackWebhookMapper
{
    /// <summary>Patches the mutable fields of <paramref name="target"/> from a PATCH body.</summary>
    public static void ApplyPatch(WebhookResponse target, Webhook patch)
    {
        target.Active = patch.Active;
        if (patch.DisplayName is not null) target.DisplayName = patch.DisplayName;
        if (patch.PayloadUrl is not null) target.PayloadUrl = patch.PayloadUrl;
        if (patch.Format is not null) target.Format = patch.Format;
        if (patch.Filters is not null) target.Filters = patch.Filters;
        if (patch.Groups is not null) target.Groups = patch.Groups;
        if (!string.IsNullOrEmpty(patch.Secret)) target.Secret = patch.Secret;
    }

    /// <summary>A copy with the secret stripped and <c>hasSecret</c> set — safe to return to clients.</summary>
    public static WebhookResponse Sanitized(WebhookResponse w) => new()
    {
        Name = w.Name, DisplayName = w.DisplayName, PayloadUrl = w.PayloadUrl, Active = w.Active,
        Format = w.Format, Filters = w.Filters, Groups = w.Groups, EnvName = w.EnvName,
        OrganizationName = w.OrganizationName, ProjectName = w.ProjectName, StackName = w.StackName,
        Secret = null, HasSecret = !string.IsNullOrEmpty(w.Secret), SecretCiphertext = "",
    };
}
