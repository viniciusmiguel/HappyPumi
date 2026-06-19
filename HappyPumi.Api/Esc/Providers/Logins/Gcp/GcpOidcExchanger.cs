#nullable enable

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.Logins.Gcp;

/// <summary>
/// Real <see cref="IGcpOidcExchanger"/>. Step 1: STS token exchange at
/// <c>https://sts.googleapis.com/v1/token</c> (grant <c>token-exchange</c>, subject token = the Pulumi JWT)
/// yields a short-lived federated access token. Step 2 (optional): impersonate a service account via
/// <c>https://iamcredentials.googleapis.com/v1/projects/-/serviceAccounts/{sa}:generateAccessToken</c>. The
/// WIF pool provider must trust HappyPumi's issuer + audience. <see cref="HttpClient"/> is injected for tests.
/// </summary>
public sealed class GcpOidcExchanger(HttpClient http) : IGcpOidcExchanger
{
    private const string TokenExchangeGrant = "urn:ietf:params:oauth:grant-type:token-exchange";
    private const string AccessTokenType = "urn:ietf:params:oauth:token-type:access_token";
    private const string JwtTokenType = "urn:ietf:params:oauth:token-type:jwt";

    public async Task<string> ExchangeForAccessTokenAsync(GcpFederationRequest request, CancellationToken ct)
    {
        var federated = await StsExchangeAsync(request, ct);
        return string.IsNullOrWhiteSpace(request.ServiceAccount)
            ? federated
            : await ImpersonateAsync(request.ServiceAccount!, federated, request.Scope, ct);
    }

    private async Task<string> StsExchangeAsync(GcpFederationRequest request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("https://sts.googleapis.com/v1/token", new
        {
            audience = request.Audience,
            grantType = TokenExchangeGrant,
            requestedTokenType = AccessTokenType,
            scope = request.Scope,
            subjectTokenType = JwtTokenType,
            subjectToken = request.SubjectToken,
        }, ct);
        response.EnsureSuccessStatusCode();
        return await ReadStringAsync(response, "access_token", ct)
            ?? throw new InvalidOperationException($"GCP STS exchange for audience '{request.Audience}' had no access_token.");
    }

    private async Task<string> ImpersonateAsync(string serviceAccount, string federatedToken, string scope, CancellationToken ct)
    {
        var url = $"https://iamcredentials.googleapis.com/v1/projects/-/serviceAccounts/{serviceAccount}:generateAccessToken";
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new { scope = new[] { scope } }),
        };
        message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", federatedToken);

        using var response = await http.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
        return await ReadStringAsync(response, "accessToken", ct)
            ?? throw new InvalidOperationException($"GCP impersonation of '{serviceAccount}' had no accessToken.");
    }

    private static async Task<string?> ReadStringAsync(HttpResponseMessage response, string property, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.TryGetProperty(property, out var value) ? value.GetString() : null;
    }
}
