#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.Logins.Azure;

/// <summary>A client-assertion exchange: present the Pulumi token as a federated credential for an AAD app.</summary>
public readonly record struct AzureClientAssertionRequest(
    string TenantId,
    string ClientId,
    string Scope,
    string Assertion);

/// <summary>
/// Exchanges a Pulumi-ESC OIDC token for an Azure AD access token via the v2.0 token endpoint using the
/// client-credentials grant with a JWT <c>client_assertion</c> (workload identity federation). Owned
/// interface over the HTTP call (CLAUDE.md) so the federation flow is testable.
/// </summary>
public interface IAzureOidcExchanger
{
    Task<string> ExchangeForAccessTokenAsync(AzureClientAssertionRequest request, CancellationToken ct);
}
