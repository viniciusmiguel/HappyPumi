#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>Maps between the <see cref="Webhook"/> wire contract and the stored webhook domain shape.</summary>
public static class WebhookMapper
{
    /// <summary>Builds the stored shape from a create/update request body.</summary>
    public static StoredWebhook FromContract(Webhook body) => new()
    {
        Name = body.Name,
        DisplayName = string.IsNullOrEmpty(body.DisplayName) ? body.Name : body.DisplayName,
        PayloadUrl = body.PayloadUrl,
        Active = body.Active,
        Format = body.Format,
        Secret = body.Secret,
        Filters = body.Filters ?? new List<string>(),
        Groups = body.Groups ?? new List<string>(),
    };

    /// <summary>Builds the wire response; the secret is never echoed, only its presence is reported.</summary>
    public static WebhookResponse ToResponse(StoredWebhook webhook, EnvCoordinates env) => new()
    {
        Name = webhook.Name,
        DisplayName = webhook.DisplayName,
        PayloadUrl = webhook.PayloadUrl,
        Active = webhook.Active,
        Format = webhook.Format,
        Filters = webhook.Filters,
        Groups = webhook.Groups,
        OrganizationName = env.Org,
        ProjectName = env.Project,
        EnvName = env.Name,
        HasSecret = !string.IsNullOrEmpty(webhook.Secret),
        SecretCiphertext = "",
    };
}
