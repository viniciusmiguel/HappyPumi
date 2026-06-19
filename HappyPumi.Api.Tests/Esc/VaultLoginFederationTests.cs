using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Oidc;
using HappyPumi.Api.Esc.Providers.Logins.Vault;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for fn::open::vault-login OIDC federation (Vault JWT auth).</summary>
public sealed class VaultLoginFederationTests
{
    private readonly EscOidcIssuer _issuer = new("https://happypumi.test/oidc", RSA.Create(2048), "kid");

    private static Dictionary<string, object?> FederatedInputs(Dictionary<string, object?> oidc) => new()
    {
        ["address"] = "https://vault.example.com",
        ["oidc"] = oidc,
    };

    [Fact]
    public async Task FederatedModeLogsInWithTheRoleAndReturnsAClientToken()
    {
        var exchanger = new FakeVaultJwtExchanger();
        var provider = new VaultLoginProvider(_issuer, exchanger);
        var inputs = FederatedInputs(new Dictionary<string, object?> { ["role"] = "esc-role", ["mount"] = "oidc" });

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(inputs, CancellationToken.None))!;

        Assert.Equal("https://vault.example.com", output["address"]);
        Assert.Equal("vault-client-token", ((Dictionary<string, object?>)output["token"]!)["fn::secret"]);
        Assert.Equal("esc-role", exchanger.LastRequest!.Value.Role);
        Assert.Equal("oidc", exchanger.LastRequest!.Value.Mount);
    }

    [Fact]
    public async Task FederatedModeRequiresARole()
    {
        var provider = new VaultLoginProvider(_issuer, new FakeVaultJwtExchanger());
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.OpenAsync(FederatedInputs(new Dictionary<string, object?>()), CancellationToken.None));
        Assert.Contains("role", ex.Message);
    }

    [Fact]
    public async Task ExchangerPostsRoleAndJwtToTheMountLoginEndpoint()
    {
        var handler = new StubHttpHandler("{\"auth\":{\"client_token\":\"s.abc123\"}}");
        var exchanger = new VaultJwtExchanger(new HttpClient(handler));

        var token = await exchanger.LoginAsync(
            new VaultJwtLoginRequest("https://vault.example.com/", "jwt", "esc-role", "the-jwt"), CancellationToken.None);

        Assert.Equal("s.abc123", token);
        Assert.Equal("https://vault.example.com/v1/auth/jwt/login", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("\"role\":\"esc-role\"", handler.LastBody);
        Assert.Contains("\"jwt\":\"the-jwt\"", handler.LastBody);
    }
}
