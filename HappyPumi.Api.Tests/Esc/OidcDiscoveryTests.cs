using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Oidc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Component tests for the public OIDC discovery + JWKS endpoints (anonymous).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class OidcDiscoveryTests(HappyPumiApp app)
{
    [Fact]
    public async Task DiscoveryDocumentAdvertisesTheIssuerAndJwksUri()
    {
        using var client = app.CreateClient();

        var doc = await client.GetFromJsonAsync<JsonElement>("/oidc/.well-known/openid-configuration");

        var issuer = doc.GetProperty("issuer").GetString()!;
        Assert.Equal($"{issuer}/.well-known/jwks", doc.GetProperty("jwks_uri").GetString());
        Assert.Contains("RS256", doc.GetProperty("id_token_signing_alg_values_supported")
            .EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task TokensMintedByTheIssuerValidateAgainstThePublishedJwks()
    {
        using var client = app.CreateClient();
        var jwksJson = await client.GetStringAsync("/oidc/.well-known/jwks");

        using var scope = app.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<IEscOidcIssuer>();
        var token = issuer.IssueToken(new EscOidcTokenRequest("sts.amazonaws.com", "pulumi:env:acme"));

        // Verify the token using ONLY the public JWKS the endpoint serves (the cloud's out-of-band path).
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = issuer.Issuer,
            ValidAudience = "sts.amazonaws.com",
            IssuerSigningKeys = new JsonWebKeySet(jwksJson).GetSigningKeys(),
        });

        Assert.True(result.IsValid);
    }
}
