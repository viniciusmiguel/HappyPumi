using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Oidc;
using HappyPumi.Api.Esc.Providers.Logins.Aws;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for fn::open::aws-login OIDC federation (assume-role-with-web-identity).</summary>
public sealed class AwsLoginFederationTests
{
    private readonly EscOidcIssuer _issuer = new("https://happypumi.test/oidc", RSA.Create(2048), "kid");
    private readonly FakeAwsStsExchanger _sts = new();

    private AwsLoginProvider Provider() => new(_issuer, _sts);

    private static Dictionary<string, object?> Oidc(params (string Key, object? Value)[] entries)
    {
        var oidc = new Dictionary<string, object?>();
        foreach (var (key, value) in entries)
            oidc[key] = value;
        return new Dictionary<string, object?> { ["oidc"] = oidc };
    }

    [Fact]
    public async Task FederatedModeExchangesAWebIdentityTokenForTempCredentials()
    {
        var inputs = Oidc(("roleArn", "arn:aws:iam::123456789012:role/pulumi-esc"), ("region", "eu-west-1"));

        var output = (Dictionary<string, object?>)(await Provider().OpenAsync(inputs, CancellationToken.None))!;

        Assert.Equal("ASIAFAKE", ((Dictionary<string, object?>)output["accessKeyId"]!)["fn::secret"]);
        Assert.Equal("fake-secret", ((Dictionary<string, object?>)output["secretAccessKey"]!)["fn::secret"]);
        Assert.Equal("fake-session", ((Dictionary<string, object?>)output["sessionToken"]!)["fn::secret"]);
        Assert.Equal("eu-west-1", output["region"]); // non-secret passthrough

        Assert.Equal("arn:aws:iam::123456789012:role/pulumi-esc", _sts.LastRequest!.Value.RoleArn);
        Assert.Equal("eu-west-1", _sts.LastRequest!.Value.Region);
    }

    [Fact]
    public async Task ExchangedTokenIsSignedByTheIssuerWithTheStsAudience()
    {
        await Provider().OpenAsync(Oidc(("roleArn", "arn:aws:iam::1:role/r")), CancellationToken.None);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            _sts.LastRequest!.Value.WebIdentityToken,
            new TokenValidationParameters
            {
                ValidIssuer = _issuer.Issuer,
                ValidAudience = "sts.amazonaws.com", // default AWS audience
                IssuerSigningKey = _issuer.SigningKey,
            });

        Assert.True(result.IsValid);
        Assert.Equal("pulumi:environments", result.ClaimsIdentity.FindFirst("sub")!.Value); // default subject
    }

    [Fact]
    public async Task FederatedModeHonoursCustomAudienceAndSubject()
    {
        await Provider().OpenAsync(
            Oidc(("roleArn", "arn:aws:iam::1:role/r"), ("audience", "my-aud"), ("subject", "pulumi:env:acme/prod")),
            CancellationToken.None);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            _sts.LastRequest!.Value.WebIdentityToken,
            new TokenValidationParameters
            {
                ValidIssuer = _issuer.Issuer,
                ValidAudience = "my-aud",
                IssuerSigningKey = _issuer.SigningKey,
            });

        Assert.True(result.IsValid);
        Assert.Equal("pulumi:env:acme/prod", result.ClaimsIdentity.FindFirst("sub")!.Value);
    }

    [Fact]
    public async Task FederatedModeRequiresRoleArn()
    {
        var ex = await Assert.ThrowsAsync<System.ArgumentException>(
            () => Provider().OpenAsync(Oidc(("sessionName", "x")), CancellationToken.None));
        Assert.Contains("roleArn", ex.Message);
    }
}
