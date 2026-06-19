#nullable enable

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.Logins.Vault;

/// <summary>
/// Real <see cref="IVaultJwtExchanger"/> over Vault's JWT-auth login endpoint
/// (<c>POST {address}/v1/auth/{mount}/login</c> with <c>{ role, jwt }</c>), returning
/// <c>auth.client_token</c>. <see cref="HttpClient"/> is injected so the request shaping is testable.
/// </summary>
public sealed class VaultJwtExchanger(HttpClient http) : IVaultJwtExchanger
{
    public async Task<string> LoginAsync(VaultJwtLoginRequest request, CancellationToken ct)
    {
        var address = request.Address.TrimEnd('/');
        var url = $"{address}/v1/auth/{request.Mount}/login";
        using var response = await http.PostAsJsonAsync(url, new { role = request.Role, jwt = request.Jwt }, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (doc.RootElement.TryGetProperty("auth", out var auth)
            && auth.TryGetProperty("client_token", out var token)
            && token.GetString() is { } value)
            return value;
        throw new InvalidOperationException($"Vault JWT login for role '{request.Role}' returned no auth.client_token.");
    }
}
