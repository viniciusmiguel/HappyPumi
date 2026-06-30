#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Vcs;

/// <summary>
/// Owned, provider-neutral seam over an external VCS host's REST API (ADR-0009). Implementations
/// (e.g. <see cref="GitHubVcsProvider"/>) wrap the host SDK/HTTP behind these calls so the endpoints stay
/// provider-agnostic. Implementations are config-gated: when the provider has no credentials configured,
/// access-status reports "not configured" and the list calls return empty rather than throwing — the VCS
/// feature is always safe to run without secrets.
/// </summary>
/// <example>
/// var provider = registry.For(integration.Kind);
/// var repos = provider is null ? [] : await provider.ListReposAsync(integration, ct);
/// </example>
public interface IVcsProvider
{
    /// <summary>True when the provider has the credentials it needs to make real calls (access-status).</summary>
    Task<bool> IsConfiguredAsync(CancellationToken ct = default);

    /// <summary>Lists the repositories reachable through the integration (empty when unconfigured).</summary>
    Task<IReadOnlyList<VcsRepo>> ListReposAsync(StoredVcsIntegration integration, CancellationToken ct = default);

    /// <summary>Lists the branches of one repository; <paramref name="repoId"/> is the provider repo id.</summary>
    Task<IReadOnlyList<VcsBranch>> ListBranchesAsync(StoredVcsIntegration integration, string repoId, CancellationToken ct = default);

    /// <summary>Lists the repos/orgs the integration can target as a destination (empty when unconfigured).</summary>
    Task<IReadOnlyList<VcsRepo>> ListRepoDestinationsAsync(StoredVcsIntegration integration, CancellationToken ct = default);
}
