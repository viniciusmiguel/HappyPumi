#nullable enable

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.CloudSetup;

/// <summary>
/// Shared outbound-HTTP shaping for the standard OAuth2 cloud providers (Azure, GCP): a form-encoded
/// token-exchange POST and a Bearer-authenticated GET. Extracted so the providers don't duplicate the
/// request/parse boilerplate (CLAUDE.md: no code duplication).
/// </summary>
internal static class CloudOAuthHttp
{
    /// <summary>POSTs the form to <paramref name="tokenUrl"/>; returns null on a non-success or token-less reply.</summary>
    public static async Task<CloudCredential?> ExchangeFormAsync(
        HttpClient http, string tokenUrl, string provider, Dictionary<string, string> form, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl) { Content = new FormUrlEncodedContent(form) };
        using var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            return null;
        var token = await resp.Content.ReadFromJsonAsync<OAuthTokenDto>(ct);
        return token?.AccessToken is null ? null : new CloudCredential(provider, token.AccessToken, token.RefreshToken);
    }

    /// <summary>GETs <paramref name="url"/> with a Bearer token; default(T) on a non-success reply.</summary>
    public static async Task<T?> GetWithBearerAsync<T>(HttpClient http, string url, string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            return default;
        return await resp.Content.ReadFromJsonAsync<T>(ct);
    }

    private sealed class OAuthTokenDto
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    }
}
