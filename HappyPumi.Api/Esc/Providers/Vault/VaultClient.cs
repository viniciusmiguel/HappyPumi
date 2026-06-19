#nullable enable

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.Vault;

/// <summary>
/// Real <see cref="IVaultClient"/> over Vault's KV-v2 HTTP API: <c>GET {address}/v1/{mount}/data/{path}</c>
/// with an <c>X-Vault-Token</c> header, returning <c>data.data.{field}</c>. A shared HttpClient is reused to
/// avoid socket exhaustion. The token is supplied per-request (just-in-time), not configured at startup.
/// </summary>
public sealed class VaultClient : IVaultClient
{
    private static readonly HttpClient Http = new();

    public async Task<string?> ReadAsync(VaultSecretRef reference, CancellationToken ct)
    {
        var address = reference.Address.TrimEnd('/');
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{address}/v1/{reference.Mount}/data/{reference.Path}");
        if (!string.IsNullOrWhiteSpace(reference.Token))
            request.Headers.Add("X-Vault-Token", reference.Token);

        using var response = await Http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        // KV v2 nests the secret payload under data.data.
        if (doc.RootElement.TryGetProperty("data", out var outer)
            && outer.TryGetProperty("data", out var data)
            && data.TryGetProperty(reference.Field, out var value))
            return value.ToString();
        return null;
    }
}
