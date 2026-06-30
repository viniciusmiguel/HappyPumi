#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.CloudSetup;

/// <summary>OAuth credential returned by a cloud provider after a code exchange (write-only; never echoed).</summary>
public sealed record CloudCredential(string Provider, string AccessToken, string? RefreshToken);

/// <summary>
/// Real-OAuth seam for the ESC cloud-setup flow (PR6), mirroring <c>IVcsProvider</c> (ADR-0009). Each
/// implementation is config-gated: with no OAuth client configured <see cref="BuildAuthorizationUrl"/>
/// returns an empty string and <see cref="ExchangeCodeAsync"/> returns null, so the endpoints degrade to
/// empty results instead of throwing.
/// </summary>
public interface ICloudSetupProvider
{
    /// <summary>"aws" | "azure" | "gcp".</summary>
    string Key { get; }

    /// <summary>Builds the provider authorization URL the browser is sent to (empty when unconfigured).</summary>
    string BuildAuthorizationUrl(string state, string? returnUrl);

    /// <summary>Exchanges an authorization code for a credential; null when unconfigured or the exchange fails.</summary>
    Task<CloudCredential?> ExchangeCodeAsync(string code, CancellationToken ct);

    /// <summary>Lists the accounts/subscriptions/projects reachable with the credential; empty on failure.</summary>
    Task<IReadOnlyList<CloudAccount>> ListAccountsAsync(CloudCredential cred, CancellationToken ct);
}
