#nullable enable

using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace HappyPumi.Api.Esc.Oidc;

/// <summary>What to mint into a Pulumi-ESC OIDC token: the cloud audience plus the federated subject.</summary>
public sealed record EscOidcTokenRequest(
    string Audience,
    string Subject,
    IReadOnlyDictionary<string, string>? AdditionalClaims = null);

/// <summary>
/// Mints short-lived, RS256-signed OIDC tokens that login providers exchange with a cloud's identity broker
/// (AWS STS, Azure AD, GCP STS, Vault JWT auth) for temporary credentials. HappyPumi is the OpenID issuer:
/// it also publishes a discovery document and JWKS so the cloud can verify the signature out-of-band. This
/// is the foundation of <em>true</em> OIDC federation (vs. the static-credential passthrough).
/// </summary>
public interface IEscOidcIssuer
{
    /// <summary>The issuer URL (the <c>iss</c> claim and the discovery document's <c>issuer</c>).</summary>
    string Issuer { get; }

    /// <summary>Mints a signed, one-hour OIDC token for the given audience/subject.</summary>
    string IssueToken(EscOidcTokenRequest request);

    /// <summary>The public signing key as a JWKS entry (public components only — never the private key).</summary>
    JsonWebKey PublicJsonWebKey();

    /// <summary>The validation key (used by tests and the discovery flow to verify minted tokens).</summary>
    SecurityKey SigningKey { get; }
}
