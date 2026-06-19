using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Oidc;
using HappyPumi.Api.Esc.Providers.Logins.Azure;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for fn::open::azure-login OIDC federation (AAD client-assertion).</summary>
public sealed class AzureLoginFederationTests
{
    private readonly EscOidcIssuer _issuer = new("https://happypumi.test/oidc", RSA.Create(2048), "kid");

    private static Dictionary<string, object?> FederatedInputs(Dictionary<string, object?>? oidc = null) => new()
    {
        ["clientId"] = "app-123",
        ["tenantId"] = "tenant-abc",
        ["subscriptionId"] = "sub-1",
        ["oidc"] = oidc ?? new Dictionary<string, object?>(),
    };

    [Fact]
    public async Task FederatedModeExchangesAClientAssertionForAnAccessToken()
    {
        var exchanger = new FakeAzureOidcExchanger();
        var provider = new AzureLoginProvider(_issuer, exchanger);

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(FederatedInputs(), CancellationToken.None))!;

        Assert.Equal("app-123", output["clientId"]);
        Assert.Equal("tenant-abc", output["tenantId"]);
        Assert.Equal("sub-1", output["subscriptionId"]);
        Assert.Equal("azure-access-token", ((Dictionary<string, object?>)output["token"]!)["fn::secret"]);

        Assert.Equal("tenant-abc", exchanger.LastRequest!.Value.TenantId);
        Assert.Equal("https://management.azure.com/.default", exchanger.LastRequest!.Value.Scope);
    }

    [Fact]
    public async Task AssertionIsSignedByTheIssuerWithTheAadExchangeAudience()
    {
        var exchanger = new FakeAzureOidcExchanger();
        await new AzureLoginProvider(_issuer, exchanger).OpenAsync(FederatedInputs(), CancellationToken.None);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            exchanger.LastRequest!.Value.Assertion,
            new TokenValidationParameters
            {
                ValidIssuer = _issuer.Issuer,
                ValidAudience = "api://AzureADTokenExchange",
                IssuerSigningKey = _issuer.SigningKey,
            });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ExchangerPostsAClientAssertionGrantToTheTenantTokenEndpoint()
    {
        var handler = new StubHttpHandler("{\"access_token\":\"aad-token\"}");
        var exchanger = new AzureOidcExchanger(new HttpClient(handler));

        var token = await exchanger.ExchangeForAccessTokenAsync(
            new AzureClientAssertionRequest("tenant-abc", "app-123", "scope/.default", "the-assertion"),
            CancellationToken.None);

        Assert.Equal("aad-token", token);
        Assert.Equal("https://login.microsoftonline.com/tenant-abc/oauth2/v2.0/token", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("grant_type=client_credentials", handler.LastBody);
        Assert.Contains("client_assertion=the-assertion", handler.LastBody);
    }
}
