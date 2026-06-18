#nullable enable

using System.Collections.Generic;
using System.Text.Json;

namespace HappyPumi.Api.Miscellaneous;

/// <summary>
/// Pure validation of an OAuth 2.0 Token Exchange request (RFC 8693) as the Pulumi CLI sends it to
/// <c>POST /api/oauth/token</c>. Kept separate from the endpoint so the wire-contract rules are unit
/// testable without booting the HTTP pipeline.
/// </summary>
/// <example><code>var errors = TokenExchange.Validate(body); if (errors.Count == 0) { /* issue */ }</code></example>
public static class TokenExchange
{
    public const string GrantType = "urn:ietf:params:oauth:grant-type:token-exchange";
    public const string SubjectTokenType = "urn:ietf:params:oauth:token-type:id_token";

    /// <summary>The four <c>requested_token_type</c> URNs the spec accepts (org/team/personal/runner).</summary>
    public static readonly IReadOnlySet<string> RequestedTokenTypes = new HashSet<string>
    {
        "urn:pulumi:token-type:access_token:organization",
        "urn:pulumi:token-type:access_token:team",
        "urn:pulumi:token-type:access_token:personal",
        "urn:pulumi:token-type:access_token:runner",
    };

    /// <summary>
    /// Returns one <c>(field, message)</c> per RFC 8693 violation; an empty list means the request is
    /// well-formed. Messages name the offending field and the expected value so callers can surface a
    /// 400 the CLI can act on.
    /// </summary>
    public static IReadOnlyList<(string Field, string Message)> Validate(IReadOnlyDictionary<string, object> body)
    {
        var errors = new List<(string, string)>();
        Require(body, "grant_type", GrantType, errors);
        Require(body, "subject_token_type", SubjectTokenType, errors);
        RequireNonEmpty(body, "subject_token", errors);
        RequireAudience(body, errors);
        RequireRequestedTokenType(body, errors);
        return errors;
    }

    /// <summary>Reads a string field, tolerating either a raw string or a System.Text.Json element.</summary>
    public static string? GetString(IReadOnlyDictionary<string, object> body, string key)
    {
        if (!body.TryGetValue(key, out var value) || value is null)
            return null;
        if (value is JsonElement element)
            return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        return value as string;
    }

    private static void Require(
        IReadOnlyDictionary<string, object> body, string key, string expected, List<(string, string)> errors)
    {
        if (GetString(body, key) != expected)
            errors.Add((key, $"'{key}' must be '{expected}'."));
    }

    private static void RequireNonEmpty(
        IReadOnlyDictionary<string, object> body, string key, List<(string, string)> errors)
    {
        if (string.IsNullOrWhiteSpace(GetString(body, key)))
            errors.Add((key, $"'{key}' is required and must be a non-empty string."));
    }

    private static void RequireAudience(IReadOnlyDictionary<string, object> body, List<(string, string)> errors)
    {
        var audience = GetString(body, "audience");
        if (string.IsNullOrWhiteSpace(audience) || !audience.StartsWith("urn:pulumi:org:"))
            errors.Add(("audience", "'audience' must identify the target org as 'urn:pulumi:org:{ORG_NAME}'."));
    }

    private static void RequireRequestedTokenType(
        IReadOnlyDictionary<string, object> body, List<(string, string)> errors)
    {
        var requested = GetString(body, "requested_token_type");
        if (requested is null || !RequestedTokenTypes.Contains(requested))
            errors.Add(("requested_token_type",
                "'requested_token_type' must be one of the urn:pulumi:token-type:access_token:* values."));
    }
}
