#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.Logins.Vault;

/// <summary>A Vault JWT-auth login: present the Pulumi token (<paramref name="Jwt"/>) for a Vault <paramref name="Role"/>.</summary>
public readonly record struct VaultJwtLoginRequest(
    string Address,
    string Mount,
    string Role,
    string Jwt);

/// <summary>
/// Logs in to HashiCorp Vault via the JWT/OIDC auth method (<c>POST {address}/v1/auth/{mount}/login</c>),
/// trading a Pulumi-ESC OIDC token for a Vault client token. Owned interface over the HTTP call (CLAUDE.md)
/// so the federation flow is testable. The Vault role's <c>bound_audiences</c>/<c>bound_issuer</c> must trust
/// HappyPumi.
/// </summary>
public interface IVaultJwtExchanger
{
    Task<string> LoginAsync(VaultJwtLoginRequest request, CancellationToken ct);
}
