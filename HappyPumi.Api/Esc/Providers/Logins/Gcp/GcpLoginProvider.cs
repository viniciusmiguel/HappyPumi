#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.Esc.Oidc;
using HappyPumi.Api.Esc.Providers.Logins;

namespace HappyPumi.Api.Esc.Providers.Logins.Gcp;

/// <summary>
/// fn::open::gcp-login — Google Cloud credentials at open time. With an <c>oidc</c> block it federates via
/// workload identity: mint a Pulumi token, exchange it (STS + optional SA impersonation) for an access token.
/// Without it, passes through a static access token.
/// </summary>
public sealed class GcpLoginProvider(IEscOidcIssuer issuer, IGcpOidcExchanger exchanger) : OidcLoginProvider(issuer)
{
    private const string DefaultScope = "https://www.googleapis.com/auth/cloud-platform";

    public override string Name => "gcp-login";
    protected override string Cloud => "Google Cloud";

    public override string Description =>
        "Provides Google Cloud credentials at open time via OIDC federation (workload identity) or a static access token.";

    // Required only in static mode; OIDC federation branches before the base enforces these.
    protected override IReadOnlyList<LoginField> Fields => new[]
    {
        new LoginField("accessToken", Secret: true, Required: true),
        new LoginField("project", Secret: false, Required: false),
    };

    protected override async Task<object?> FederateAsync(
        IReadOnlyDictionary<string, object?> inputs, IReadOnlyDictionary<string, object?> oidc, CancellationToken ct)
    {
        // The audience is the WIF pool provider resource; there is no sensible default, so require it.
        var audience = EscProviderInputs.Require<string>(oidc, Name, "audience");
        var scope = AsString(oidc, "scope") ?? DefaultScope;

        var accessToken = await exchanger.ExchangeForAccessTokenAsync(new GcpFederationRequest(
            Audience: audience,
            SubjectToken: MintToken(oidc, audience),
            Scope: scope,
            ServiceAccount: AsString(oidc, "serviceAccount")), ct);

        var output = new Dictionary<string, object?> { ["accessToken"] = EscProviderInputs.Secret(accessToken) };
        if (AsString(inputs, "project") is { } project)
            output["project"] = project;
        return output;
    }

    protected override EscSchemaSchema OidcInputSchema => new()
    {
        Type = "object",
        Description = "OIDC federation: exchange a Pulumi-issued token for a GCP access token via workload identity.",
        Required = new List<string> { "audience" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["audience"] = new() { Type = "string", Description = "The workload identity pool provider resource name." },
            ["serviceAccount"] = new() { Type = "string", Description = "Service account to impersonate (optional)." },
            ["scope"] = new() { Type = "string", Description = $"OAuth scope (default '{DefaultScope}')." },
            ["subject"] = new() { Type = "string", Description = "Token subject claim for the pool provider's attribute mapping." },
        },
    };
}
