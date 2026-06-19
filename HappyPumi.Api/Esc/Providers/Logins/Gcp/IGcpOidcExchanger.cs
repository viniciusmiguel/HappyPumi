#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.Logins.Gcp;

/// <summary>
/// A workload-identity-federation exchange: trade the Pulumi token for a GCP access token at
/// <paramref name="Audience"/> (the WIF pool provider resource), optionally impersonating
/// <paramref name="ServiceAccount"/>.
/// </summary>
public readonly record struct GcpFederationRequest(
    string Audience,
    string SubjectToken,
    string Scope,
    string? ServiceAccount);

/// <summary>
/// Exchanges a Pulumi-ESC OIDC token for a Google Cloud access token via the STS token-exchange endpoint,
/// then (when a service account is given) impersonates it via the IAM Credentials API. Owned interface over
/// the HTTP calls (CLAUDE.md) so the federation flow is testable.
/// </summary>
public interface IGcpOidcExchanger
{
    Task<string> ExchangeForAccessTokenAsync(GcpFederationRequest request, CancellationToken ct);
}
