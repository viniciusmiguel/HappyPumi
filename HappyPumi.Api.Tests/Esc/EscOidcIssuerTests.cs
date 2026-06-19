using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Oidc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the OIDC token issuer that backs login-provider federation.</summary>
public sealed class EscOidcIssuerTests
{
    private static EscOidcIssuer NewIssuer()
        => new("https://happypumi.test/oidc", RSA.Create(2048), "test-kid");

    [Fact]
    public async Task IssuedTokenValidatesAgainstTheSigningKey()
    {
        var issuer = NewIssuer();
        var token = issuer.IssueToken(new EscOidcTokenRequest("sts.amazonaws.com", "pulumi:env:acme"));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = issuer.Issuer,
            ValidAudience = "sts.amazonaws.com",
            IssuerSigningKey = issuer.SigningKey,
        });

        Assert.True(result.IsValid);
        Assert.Equal("pulumi:env:acme", result.ClaimsIdentity.FindFirst("sub")!.Value);
    }

    [Fact]
    public void PublicJsonWebKeyExposesOnlyPublicComponents()
    {
        var jwk = NewIssuer().PublicJsonWebKey();

        Assert.Equal("RSA", jwk.Kty);
        Assert.Equal("sig", jwk.Use);
        Assert.Equal("test-kid", jwk.Kid);
        Assert.False(string.IsNullOrEmpty(jwk.N));
        Assert.False(string.IsNullOrEmpty(jwk.E));
        Assert.True(string.IsNullOrEmpty(jwk.D), "JWKS must never expose the private exponent");
    }

    [Fact]
    public void ConfiguredKeyRoundTripsThroughPem()
    {
        var rsa = RSA.Create(2048);
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
            {
                ["Esc:Oidc:Issuer"] = "https://configured/oidc",
                ["Esc:Oidc:PrivateKeyPem"] = rsa.ExportRSAPrivateKeyPem(),
            }).Build();

        var issuer = EscOidcIssuer.FromConfiguration(config);

        Assert.Equal("https://configured/oidc", issuer.Issuer);
        // Same key material -> same public modulus in the JWKS.
        var expected = Base64UrlEncoder.Encode(rsa.ExportParameters(false).Modulus);
        Assert.Equal(expected, issuer.PublicJsonWebKey().N);
    }
}
