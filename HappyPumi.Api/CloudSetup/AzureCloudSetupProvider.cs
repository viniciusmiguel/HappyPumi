#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using Microsoft.Extensions.Configuration;

namespace HappyPumi.Api.CloudSetup;

/// <summary>
/// Real-OAuth Azure cloud-setup provider (PR6). Builds the Microsoft identity-platform authorization URL,
/// exchanges the code at the v2.0 token endpoint, and lists ARM subscriptions. Config-gated on
/// <c>CloudSetup:Azure:{TenantId,ClientId,ClientSecret,RedirectUri}</c>: unconfigured → empty URL / null
/// credential / empty accounts.
/// </summary>
public sealed class AzureCloudSetupProvider(HttpClient http, IConfiguration config) : ICloudSetupProvider
{
    private const string DefaultScopes = "https://management.azure.com/user_impersonation offline_access";
    private const string SubscriptionsUrl = "https://management.azure.com/subscriptions?api-version=2020-01-01";

    private readonly string _tenant = config["CloudSetup:Azure:TenantId"] ?? "organizations";
    private readonly string? _clientId = config["CloudSetup:Azure:ClientId"];
    private readonly string? _clientSecret = config["CloudSetup:Azure:ClientSecret"];
    private readonly string _redirectUri = config["CloudSetup:Azure:RedirectUri"] ?? "";

    public string Key => "azure";

    private bool IsConfigured => !string.IsNullOrWhiteSpace(_clientId);

    public string BuildAuthorizationUrl(string state, string? returnUrl)
    {
        if (!IsConfigured)
            return "";
        var redirect = string.IsNullOrWhiteSpace(returnUrl) ? _redirectUri : returnUrl;
        var query = $"client_id={Uri.EscapeDataString(_clientId!)}&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(redirect)}&response_mode=query" +
            $"&scope={Uri.EscapeDataString(DefaultScopes)}&state={Uri.EscapeDataString(state)}";
        return $"https://login.microsoftonline.com/{_tenant}/oauth2/v2.0/authorize?{query}";
    }

    public Task<CloudCredential?> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        if (!IsConfigured)
            return Task.FromResult<CloudCredential?>(null);
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _clientId!,
            ["client_secret"] = _clientSecret ?? "",
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _redirectUri,
            ["scope"] = DefaultScopes,
        };
        return CloudOAuthHttp.ExchangeFormAsync(http, $"https://login.microsoftonline.com/{_tenant}/oauth2/v2.0/token", Key, form, ct);
    }

    public async Task<IReadOnlyList<CloudAccount>> ListAccountsAsync(CloudCredential cred, CancellationToken ct)
    {
        var subs = await CloudOAuthHttp.GetWithBearerAsync<SubscriptionList>(http, SubscriptionsUrl, cred.AccessToken, ct);
        return subs?.Value is null ? Array.Empty<CloudAccount>() : subs.Value.Select(ToAccount).ToArray();
    }

    private static CloudAccount ToAccount(SubscriptionDto s)
        => new() { Id = s.SubscriptionId ?? "", Name = s.DisplayName ?? s.SubscriptionId ?? "" };

    private sealed class SubscriptionList
    {
        [JsonPropertyName("value")] public List<SubscriptionDto>? Value { get; set; }
    }

    private sealed class SubscriptionDto
    {
        [JsonPropertyName("subscriptionId")] public string? SubscriptionId { get; set; }
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    }
}
