#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.Esc.Oidc;
using HappyPumi.Api.Esc.Providers.Logins;

namespace HappyPumi.Api.Esc.Providers.Logins.Azure;

/// <summary>
/// fn::open::azure-login — Azure credentials at open time. With an <c>oidc</c> block it federates: mint a
/// Pulumi token and exchange it (client-assertion) for an AAD access token, exposing that plus the identity
/// context. Without it, passes through static service-principal credentials.
/// </summary>
public sealed class AzureLoginProvider(IEscOidcIssuer issuer, IAzureOidcExchanger exchanger) : OidcLoginProvider(issuer)
{
    private const string DefaultAudience = "api://AzureADTokenExchange";
    private const string DefaultScope = "https://management.azure.com/.default";

    public override string Name => "azure-login";
    protected override string Cloud => "Azure";

    public override string Description =>
        "Provides Azure credentials at open time via OIDC federation (client-assertion) or static service-principal credentials.";

    // Required only in static mode; OIDC federation branches before the base enforces these.
    protected override IReadOnlyList<LoginField> Fields => new[]
    {
        new LoginField("clientId", Secret: false, Required: true),
        new LoginField("clientSecret", Secret: true, Required: true),
        new LoginField("tenantId", Secret: false, Required: true),
        new LoginField("subscriptionId", Secret: false, Required: false),
    };

    protected override async Task<object?> FederateAsync(
        IReadOnlyDictionary<string, object?> inputs, IReadOnlyDictionary<string, object?> oidc, CancellationToken ct)
    {
        var clientId = EscProviderInputs.Require<string>(inputs, Name, "clientId");
        var tenantId = EscProviderInputs.Require<string>(inputs, Name, "tenantId");

        var accessToken = await exchanger.ExchangeForAccessTokenAsync(new AzureClientAssertionRequest(
            TenantId: tenantId,
            ClientId: clientId,
            Scope: AsString(oidc, "scope") ?? DefaultScope,
            Assertion: MintToken(oidc, DefaultAudience)), ct);

        var output = new Dictionary<string, object?>
        {
            ["clientId"] = clientId,
            ["tenantId"] = tenantId,
            ["token"] = EscProviderInputs.Secret(accessToken),
        };
        if (AsString(inputs, "subscriptionId") is { } subscriptionId)
            output["subscriptionId"] = subscriptionId;
        return output;
    }

    protected override EscSchemaSchema OidcInputSchema => new()
    {
        Type = "object",
        Description = "OIDC federation: exchange a Pulumi-issued token for an Azure AD access token.",
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["scope"] = new() { Type = "string", Description = $"OAuth scope (default '{DefaultScope}')." },
            ["audience"] = new() { Type = "string", Description = $"Token audience (default '{DefaultAudience}')." },
            ["subject"] = new() { Type = "string", Description = "Token subject claim for the federated credential's trust." },
        },
    };
}
