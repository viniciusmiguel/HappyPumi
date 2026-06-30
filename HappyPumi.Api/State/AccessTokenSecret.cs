#nullable enable

using System;
using System.Security.Cryptography;

namespace HappyPumi.Api.State;

/// <summary>
/// Generates Pulumi-style access-token plaintexts and the SHA-256 hashes persisted in their place. The
/// plaintext (<c>pul-&lt;base64url(32 random bytes)&gt;</c>) is returned once at issue time; only the hash is
/// stored, so a leaked store cannot reconstruct usable tokens.
/// </summary>
public static class AccessTokenSecret
{
    /// <summary>Issues a fresh token, returning its plaintext and the hash to persist.</summary>
    public static (string Plaintext, string Hash) Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var plaintext = "pul-" + Base64Url(bytes);
        return (plaintext, Hash(plaintext));
    }

    /// <summary>SHA-256 hex of a token plaintext; used to look up / store a token without the secret.</summary>
    public static string Hash(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException($"token plaintext must be non-empty, got '{plaintext}'", nameof(plaintext));
        var digest = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexStringLower(digest);
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
