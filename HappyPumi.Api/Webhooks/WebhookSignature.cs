#nullable enable

using System;
using System.Security.Cryptography;
using System.Text;

namespace HappyPumi.Api.Webhooks;

/// <summary>
/// Computes the <c>X-Pulumi-Signature</c> header value: an HMAC-SHA256 of the request body keyed by the
/// webhook secret, formatted as <c>sha256=&lt;lowercase-hex&gt;</c> so receivers can verify authenticity.
/// </summary>
/// <example><code>request.Headers.Add("X-Pulumi-Signature", WebhookSignature.Sign(body, secret));</code></example>
public static class WebhookSignature
{
    public const string HeaderName = "X-Pulumi-Signature";

    public static string Sign(string body, string secret)
    {
        if (string.IsNullOrEmpty(secret))
            throw new ArgumentException("A non-empty secret is required to sign a webhook body.", nameof(secret));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return "sha256=" + Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }
}
