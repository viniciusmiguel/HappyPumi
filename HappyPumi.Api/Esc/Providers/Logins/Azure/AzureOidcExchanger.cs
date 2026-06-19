#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.Logins.Azure;

/// <summary>
/// Real <see cref="IAzureOidcExchanger"/> over the AAD v2.0 token endpoint
/// (<c>POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token</c>) with grant
/// <c>client_credentials</c> and a <c>client_assertion</c> of type <c>jwt-bearer</c>. The app registration's
/// federated credential must trust HappyPumi's issuer + the token audience. The <see cref="HttpClient"/> is
/// injected so the request shaping is testable with a fake handler.
/// </summary>
public sealed class AzureOidcExchanger(HttpClient http) : IAzureOidcExchanger
{
    private const string JwtBearerAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

    public async Task<string> ExchangeForAccessTokenAsync(AzureClientAssertionRequest request, CancellationToken ct)
    {
        var url = $"https://login.microsoftonline.com/{request.TenantId}/oauth2/v2.0/token";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = request.ClientId,
            ["scope"] = request.Scope,
            ["client_assertion_type"] = JwtBearerAssertionType,
            ["client_assertion"] = request.Assertion,
        });

        using var response = await http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (doc.RootElement.TryGetProperty("access_token", out var token) && token.GetString() is { } value)
            return value;
        throw new InvalidOperationException($"Azure AD token response for client '{request.ClientId}' had no access_token.");
    }
}
