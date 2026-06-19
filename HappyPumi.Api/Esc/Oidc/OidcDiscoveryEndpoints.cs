#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;

namespace HappyPumi.Api.Esc.Oidc;

// Public OIDC discovery surface so a cloud's identity broker (AWS STS, Azure AD, GCP STS, Vault) can fetch
// HappyPumi's signing keys and verify the tokens its login providers mint. Not part of the Pulumi OpenAPI
// spec (the real service exposes the equivalent under api.pulumi.com/oidc); reverse-engineered from how OIDC
// federation works (ADR-0008). Both endpoints are anonymous — they expose only public key material.

/// <summary>GET /oidc/.well-known/openid-configuration — the OpenID Connect discovery document.</summary>
public sealed class OidcDiscoveryEndpoint(IEscOidcIssuer issuer) : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Get("/oidc/.well-known/openid-configuration");
        AllowAnonymous();
        Description(b => b.WithTags("Oidc").WithName("GetOidcDiscovery"));
    }

    public override async Task HandleAsync(CancellationToken ct)
        => await Send.OkAsync(new
        {
            issuer = issuer.Issuer,
            jwks_uri = $"{issuer.Issuer}/.well-known/jwks",
            response_types_supported = new[] { "id_token" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            scopes_supported = new[] { "openid" },
            claims_supported = new[] { "sub", "aud", "exp", "iat", "iss" },
        }, ct);
}

/// <summary>GET /oidc/.well-known/jwks — the JSON Web Key Set with the public signing key.</summary>
public sealed class OidcJwksEndpoint(IEscOidcIssuer issuer) : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Get("/oidc/.well-known/jwks");
        AllowAnonymous();
        Description(b => b.WithTags("Oidc").WithName("GetOidcJwks"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var jwk = issuer.PublicJsonWebKey();
        await Send.OkAsync(new
        {
            keys = new[] { new { kty = jwk.Kty, use = jwk.Use, alg = jwk.Alg, kid = jwk.Kid, n = jwk.N, e = jwk.E } },
        }, ct);
    }
}
