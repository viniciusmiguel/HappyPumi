#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IVcsIntegrationStore"/> (ADR-0005), keyed by (org, id).</summary>
public sealed class InMemoryVcsIntegrationStore : IVcsIntegrationStore
{
    private readonly ConcurrentDictionary<(string Org, string Id), StoredVcsIntegration> _state = new();

    public StoredVcsIntegration Create(StoredVcsIntegration integration)
    {
        var stored = new StoredVcsIntegration
        {
            Id = Guid.NewGuid().ToString(),
            Org = integration.Org,
            Kind = integration.Kind,
            Name = integration.Name,
            BaseUrl = integration.BaseUrl,
            AccountName = integration.AccountName,
            AccountId = integration.AccountId,
            AvatarUrl = integration.AvatarUrl,
            AzureProject = integration.AzureProject,
            Settings = integration.Settings,
            Created = integration.Created,
            CreatedBy = integration.CreatedBy,
        };
        _state[(stored.Org, stored.Id)] = stored;
        return stored;
    }

    public IReadOnlyList<StoredVcsIntegration> List(string org, string? kind = null)
        => _state.Values
            .Where(i => i.Org == org && (kind is null || i.Kind == kind))
            .OrderBy(i => i.Created)
            .ToArray();

    public StoredVcsIntegration? Get(string org, string id)
        => _state.TryGetValue((org, id), out var found) ? found : null;

    public StoredVcsIntegration? UpdateSettings(string org, string id, VcsIntegrationSettings settings)
    {
        if (!_state.TryGetValue((org, id), out var found))
            return null;
        found.Settings = settings;
        return found;
    }

    public bool Delete(string org, string id) => _state.TryRemove((org, id), out _);
}
