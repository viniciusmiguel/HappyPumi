#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc.Oidc;

namespace HappyPumi.Api.Esc.Providers.Logins;

/// <summary>
/// Base for login providers that support <em>OIDC federation</em>: when the inputs carry an <c>oidc</c> block,
/// mint a Pulumi web-identity token (via <see cref="IEscOidcIssuer"/>) and let the subclass exchange it with
/// the cloud's identity broker for short-lived credentials. Without an <c>oidc</c> block it falls back to the
/// static-credential pass-through of <see cref="StaticLoginProvider"/>.
/// </summary>
public abstract class OidcLoginProvider(IEscOidcIssuer issuer) : StaticLoginProvider
{
    /// <summary>Default <c>sub</c> claim; override per-environment trust via the <c>oidc.subject</c> input.</summary>
    protected const string DefaultSubject = "pulumi:environments";

    public override Task<object?> OpenAsync(IReadOnlyDictionary<string, object?> inputs, CancellationToken ct)
        => inputs.GetValueOrDefault("oidc") is Dictionary<string, object?> oidc
            ? FederateAsync(inputs, oidc, ct)
            : base.OpenAsync(inputs, ct);

    public override EscSchemaSchema Inputs
    {
        get
        {
            var schema = base.Inputs;
            schema.Properties!["oidc"] = OidcInputSchema;
            return schema;
        }
    }

    /// <summary>Exchanges a freshly minted token for credentials. <paramref name="oidc"/> is the input's <c>oidc</c> map.</summary>
    protected abstract Task<object?> FederateAsync(
        IReadOnlyDictionary<string, object?> inputs, IReadOnlyDictionary<string, object?> oidc, CancellationToken ct);

    /// <summary>JSON-Schema for the provider's <c>oidc</c> federation block.</summary>
    protected abstract EscSchemaSchema OidcInputSchema { get; }

    /// <summary>Mints a token for this exchange, honouring <c>oidc.audience</c> / <c>oidc.subject</c> overrides.</summary>
    protected string MintToken(IReadOnlyDictionary<string, object?> oidc, string defaultAudience)
        => issuer.IssueToken(new EscOidcTokenRequest(
            Audience: AsString(oidc, "audience") ?? defaultAudience,
            Subject: AsString(oidc, "subject") ?? DefaultSubject));

    protected static string? AsString(IReadOnlyDictionary<string, object?> map, string key)
        => map.GetValueOrDefault(key) as string;

    protected static int? AsInt(IReadOnlyDictionary<string, object?> map, string key)
        => map.GetValueOrDefault(key) switch
        {
            null => null,
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) => v,
            var other => throw new ArgumentException($"'{key}' must be an integer; got {EscProviderInputs.Describe(other)}."),
        };
}
