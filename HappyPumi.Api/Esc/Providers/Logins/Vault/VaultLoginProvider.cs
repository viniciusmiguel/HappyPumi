#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.Esc.Oidc;
using HappyPumi.Api.Esc.Providers.Logins;

namespace HappyPumi.Api.Esc.Providers.Logins.Vault;

/// <summary>
/// fn::open::vault-login — HashiCorp Vault credentials at open time. With an <c>oidc</c> block it federates
/// via Vault's JWT auth method: mint a Pulumi token and exchange it for a Vault client token. Without it,
/// passes through a static address + token.
/// </summary>
public sealed class VaultLoginProvider(IEscOidcIssuer issuer, IVaultJwtExchanger exchanger) : OidcLoginProvider(issuer)
{
    private const string DefaultMount = "jwt";
    private const string DefaultAudience = "vault";

    public override string Name => "vault-login";
    protected override string Cloud => "HashiCorp Vault";

    public override string Description =>
        "Provides HashiCorp Vault credentials at open time via OIDC federation (JWT auth) or a static token.";

    // Required only in static mode; OIDC federation branches before the base enforces these.
    protected override IReadOnlyList<LoginField> Fields => new[]
    {
        new LoginField("address", Secret: false, Required: true),
        new LoginField("token", Secret: true, Required: true),
    };

    protected override async Task<object?> FederateAsync(
        IReadOnlyDictionary<string, object?> inputs, IReadOnlyDictionary<string, object?> oidc, CancellationToken ct)
    {
        var address = EscProviderInputs.Require<string>(inputs, Name, "address");
        var role = EscProviderInputs.Require<string>(oidc, Name, "role");

        var clientToken = await exchanger.LoginAsync(new VaultJwtLoginRequest(
            Address: address,
            Mount: AsString(oidc, "mount") ?? DefaultMount,
            Role: role,
            Jwt: MintToken(oidc, DefaultAudience)), ct);

        return new Dictionary<string, object?>
        {
            ["address"] = address,
            ["token"] = EscProviderInputs.Secret(clientToken),
        };
    }

    protected override EscSchemaSchema OidcInputSchema => new()
    {
        Type = "object",
        Description = "OIDC federation: exchange a Pulumi-issued token for a Vault client token via JWT auth.",
        Required = new List<string> { "role" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["role"] = new() { Type = "string", Description = "The Vault JWT-auth role to log in as." },
            ["mount"] = new() { Type = "string", Description = $"The JWT-auth mount path (default '{DefaultMount}')." },
            ["audience"] = new() { Type = "string", Description = $"Token audience (default '{DefaultAudience}')." },
            ["subject"] = new() { Type = "string", Description = "Token subject claim for the role's bound_subject." },
        },
    };
}
