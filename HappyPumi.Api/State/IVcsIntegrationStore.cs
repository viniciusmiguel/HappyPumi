#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for provider-neutral VCS integration records (ADR-0005 / ADR-0009). The per-provider
/// endpoints are thin CRUD over this store; <c>kind</c> ("github" | "github-enterprise" | "azure-devops")
/// scopes list/filter operations.
/// </summary>
public interface IVcsIntegrationStore
{
    /// <summary>Persists a new integration, assigning its <see cref="StoredVcsIntegration.Id"/>.</summary>
    StoredVcsIntegration Create(StoredVcsIntegration integration);

    /// <summary>Lists an org's integrations; pass <paramref name="kind"/> to filter (null = all kinds).</summary>
    IReadOnlyList<StoredVcsIntegration> List(string org, string? kind = null);

    StoredVcsIntegration? Get(string org, string id);

    /// <summary>Replaces the settings of an existing record; returns null if it is missing.</summary>
    StoredVcsIntegration? UpdateSettings(string org, string id, VcsIntegrationSettings settings);

    bool Delete(string org, string id);
}
