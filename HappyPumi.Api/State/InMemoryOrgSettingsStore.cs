#nullable enable

using System;
using System.Collections.Concurrent;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IOrgSettingsStore"/> (ADR-0005), keyed by org. Used by unit tests.</summary>
public sealed class InMemoryOrgSettingsStore : IOrgSettingsStore
{
    private readonly ConcurrentDictionary<string, StoredOrgSettings> _byOrg = new();

    public StoredOrgSettings Get(string org)
        => _byOrg.TryGetValue(org, out var settings) ? settings : new StoredOrgSettings { Org = org };

    public StoredOrgSettings Update(string org, Action<StoredOrgSettings> mutate)
    {
        var settings = _byOrg.GetOrAdd(org, o => new StoredOrgSettings { Org = o });
        lock (settings)
            mutate(settings);
        return settings;
    }
}
