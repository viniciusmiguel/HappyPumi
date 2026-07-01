#nullable enable

using System;
using System.Collections.Concurrent;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IAuditExportConfigStore"/> (ADR-0005), keyed by org. Used by unit tests.</summary>
public sealed class InMemoryAuditExportConfigStore : IAuditExportConfigStore
{
    private readonly ConcurrentDictionary<string, StoredAuditExportConfig> _byOrg = new();

    public StoredAuditExportConfig Get(string org)
        => _byOrg.TryGetValue(org, out var config) ? config : new StoredAuditExportConfig { Org = org };

    public StoredAuditExportConfig Upsert(string org, Action<StoredAuditExportConfig> mutate)
    {
        var config = _byOrg.GetOrAdd(org, o => new StoredAuditExportConfig { Org = o });
        lock (config)
            mutate(config);
        return config;
    }

    public bool Delete(string org) => _byOrg.TryRemove(org, out _);
}
