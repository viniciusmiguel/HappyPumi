#nullable enable

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HappyPumi.Api.Esc.Oidc;

/// <summary>
/// Default <see cref="IEscOidcIssuer"/>: an RSA key signs RS256 tokens. The key is loaded from
/// <c>Esc:Oidc:PrivateKeyPem</c> when configured (stable across restarts, required for a real federation
/// trust) and generated in-memory otherwise (fine for dev/test). Token lifetime is one hour — long enough
/// to complete a credential exchange, short enough to limit replay.
/// </summary>
public sealed class EscOidcIssuer : IEscOidcIssuer
{
    private const int TokenLifetimeMinutes = 60;

    private readonly RSA _rsa;
    private readonly RsaSecurityKey _key;
    private readonly SigningCredentials _signing;

    public EscOidcIssuer(string issuer, RSA rsa, string keyId)
    {
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException($"OIDC issuer URL is required; got '{issuer}'.", nameof(issuer));
        Issuer = issuer;
        _rsa = rsa;
        _key = new RsaSecurityKey(rsa) { KeyId = keyId };
        _signing = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256);
    }

    public string Issuer { get; }

    public SecurityKey SigningKey => _key;

    public string IssueToken(EscOidcTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Audience))
            throw new ArgumentException($"OIDC token audience is required; got '{request.Audience}'.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new ArgumentException($"OIDC token subject is required; got '{request.Subject}'.", nameof(request));

        var now = DateTime.UtcNow;
        var claims = new Dictionary<string, object> { ["sub"] = request.Subject };
        if (request.AdditionalClaims is not null)
            foreach (var (name, value) in request.AdditionalClaims)
                claims[name] = value;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = request.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(TokenLifetimeMinutes),
            SigningCredentials = _signing,
            Claims = claims,
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    // Export only the public modulus/exponent — the private key must never leave the process via JWKS.
    public JsonWebKey PublicJsonWebKey()
    {
        var p = _rsa.ExportParameters(includePrivateParameters: false);
        return new JsonWebKey
        {
            Kty = "RSA",
            Use = "sig",
            Alg = SecurityAlgorithms.RsaSha256,
            Kid = _key.KeyId,
            N = Base64UrlEncoder.Encode(p.Modulus),
            E = Base64UrlEncoder.Encode(p.Exponent),
        };
    }

    /// <summary>Builds the issuer from configuration, generating an ephemeral key when none is supplied.</summary>
    public static EscOidcIssuer FromConfiguration(IConfiguration config)
    {
        var issuer = config.GetValue<string?>("Esc:Oidc:Issuer") ?? "https://happypumi.local/oidc";
        var rsa = RSA.Create(2048);
        var pem = config.GetValue<string?>("Esc:Oidc:PrivateKeyPem");
        if (!string.IsNullOrWhiteSpace(pem))
            rsa.ImportFromPem(pem);
        var keyId = config.GetValue<string?>("Esc:Oidc:KeyId") ?? Thumbprint(rsa);
        return new EscOidcIssuer(issuer, rsa, keyId);
    }

    // Deterministic kid from the public key so the JWKS entry is stable for a given key.
    private static string Thumbprint(RSA rsa)
    {
        var modulus = rsa.ExportParameters(false).Modulus!;
        return Base64UrlEncoder.Encode(SHA256.HashData(modulus))[..16];
    }
}
