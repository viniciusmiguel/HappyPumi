#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Derives an OIDC issuer's TLS certificate thumbprints by walking its discovery document → JWKS.
/// Used by the regenerate-thumbprints flow when a provider rotates its signing certificates.
/// </summary>
public interface IOidcThumbprintFetcher
{
    /// <summary>
    /// GET <c>{issuerUrl}/.well-known/openid-configuration</c> → <c>jwks_uri</c> → JWKS; for each key's
    /// <c>x5c[0]</c> (base64 DER cert) computes the SHA-1 thumbprint (uppercase hex, no separators).
    /// Returns the derived thumbprints (empty if none derivable). Never throws — network/parse errors → empty.
    /// </summary>
    Task<IReadOnlyList<string>> FetchAsync(string issuerUrl, CancellationToken ct);
}

/// <summary>Typed-HttpClient implementation of <see cref="IOidcThumbprintFetcher"/>.</summary>
public sealed class OidcThumbprintFetcher(System.Net.Http.HttpClient http) : IOidcThumbprintFetcher
{
    public async Task<IReadOnlyList<string>> FetchAsync(string issuerUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(issuerUrl)) return [];
        try
        {
            var jwksUri = await DiscoverJwksUriAsync(issuerUrl, ct);
            return jwksUri is null ? [] : await FetchThumbprintsAsync(jwksUri, ct);
        }
        catch
        {
            // Fail-fast/soft: a bad provider, DNS failure, or malformed JSON yields no thumbprints, not a 500.
            return [];
        }
    }

    /// <summary>Computes the SHA-1 thumbprint (uppercase hex) of a base64-encoded DER certificate.</summary>
    public static string Thumbprint(string x5cBase64)
    {
        var der = Convert.FromBase64String(x5cBase64);
        return Convert.ToHexString(SHA1.HashData(der));
    }

    private async Task<string?> DiscoverJwksUriAsync(string issuerUrl, CancellationToken ct)
    {
        var discoveryUrl = issuerUrl.TrimEnd('/') + "/.well-known/openid-configuration";
        await using var stream = await http.GetStreamAsync(discoveryUrl, ct);
        using var doc = await JsonDocument.ParseAsync(stream, default, ct);
        return doc.RootElement.TryGetProperty("jwks_uri", out var uri) ? uri.GetString() : null;
    }

    private async Task<IReadOnlyList<string>> FetchThumbprintsAsync(string jwksUri, CancellationToken ct)
    {
        await using var stream = await http.GetStreamAsync(jwksUri, ct);
        using var doc = await JsonDocument.ParseAsync(stream, default, ct);
        if (!doc.RootElement.TryGetProperty("keys", out var keys) || keys.ValueKind != JsonValueKind.Array)
            return [];
        return keys.EnumerateArray().Select(ThumbprintOf).Where(t => t is not null).Select(t => t!).ToList();
    }

    private static string? ThumbprintOf(JsonElement key)
    {
        if (!key.TryGetProperty("x5c", out var chain) || chain.ValueKind != JsonValueKind.Array || chain.GetArrayLength() == 0)
            return null;
        var leaf = chain[0].GetString();
        return string.IsNullOrEmpty(leaf) ? null : Thumbprint(leaf);
    }
}
