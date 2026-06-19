using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Oidc;
using HappyPumi.Api.Esc.Providers.Logins.Gcp;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for fn::open::gcp-login OIDC federation (workload identity).</summary>
public sealed class GcpLoginFederationTests
{
    private const string PoolAudience = "//iam.googleapis.com/projects/1/locations/global/workloadIdentityPools/p/providers/pr";
    private readonly EscOidcIssuer _issuer = new("https://happypumi.test/oidc", RSA.Create(2048), "kid");

    private static Dictionary<string, object?> FederatedInputs(Dictionary<string, object?> oidc) => new()
    {
        ["project"] = "my-project",
        ["oidc"] = oidc,
    };

    [Fact]
    public async Task FederatedModeExchangesTheSubjectTokenForAnAccessToken()
    {
        var exchanger = new FakeGcpOidcExchanger();
        var provider = new GcpLoginProvider(_issuer, exchanger);
        var inputs = FederatedInputs(new Dictionary<string, object?>
        {
            ["audience"] = PoolAudience,
            ["serviceAccount"] = "sa@my-project.iam.gserviceaccount.com",
        });

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(inputs, CancellationToken.None))!;

        Assert.Equal("gcp-access-token", ((Dictionary<string, object?>)output["accessToken"]!)["fn::secret"]);
        Assert.Equal("my-project", output["project"]);
        Assert.Equal(PoolAudience, exchanger.LastRequest!.Value.Audience);
        Assert.Equal("sa@my-project.iam.gserviceaccount.com", exchanger.LastRequest!.Value.ServiceAccount);
    }

    [Fact]
    public async Task FederatedModeRequiresTheWorkloadIdentityAudience()
    {
        var provider = new GcpLoginProvider(_issuer, new FakeGcpOidcExchanger());
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.OpenAsync(FederatedInputs(new Dictionary<string, object?>()), CancellationToken.None));
        Assert.Contains("audience", ex.Message);
    }

    [Fact]
    public async Task ExchangerDoesStsOnlyWithoutAServiceAccount()
    {
        var handler = new StubHttpHandler("{\"access_token\":\"federated\"}");
        var exchanger = new GcpOidcExchanger(new HttpClient(handler));

        var token = await exchanger.ExchangeForAccessTokenAsync(
            new GcpFederationRequest(PoolAudience, "subject-jwt", "scope", ServiceAccount: null), CancellationToken.None);

        Assert.Equal("federated", token);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("https://sts.googleapis.com/v1/token", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ExchangerImpersonatesAServiceAccountAfterTheStsExchange()
    {
        var handler = new StubHttpHandler("{\"access_token\":\"federated\"}").ThenRespondWith("{\"accessToken\":\"impersonated\"}");
        var exchanger = new GcpOidcExchanger(new HttpClient(handler));

        var token = await exchanger.ExchangeForAccessTokenAsync(
            new GcpFederationRequest(PoolAudience, "subject-jwt", "scope", "sa@p.iam.gserviceaccount.com"), CancellationToken.None);

        Assert.Equal("impersonated", token);
        Assert.Equal(2, handler.CallCount);
        Assert.Contains("generateAccessToken", handler.LastRequest!.RequestUri!.ToString());
    }
}
