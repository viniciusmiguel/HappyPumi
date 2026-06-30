#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="ISamlConfigStore"/> (ADR-0005), keyed by org. Used by unit tests.</summary>
public sealed class InMemorySamlConfigStore : ISamlConfigStore
{
    private readonly ConcurrentDictionary<string, StoredSamlConfig> _byOrg = new();

    public StoredSamlConfig? Get(string org)
        => _byOrg.TryGetValue(org, out var config) ? config : null;

    public StoredSamlConfig Upsert(StoredSamlConfig config)
    {
        _byOrg[config.Org] = config;
        return config;
    }

    public IReadOnlyList<string> ListAdmins(string org)
        => Get(org)?.Admins.ToArray() ?? Array.Empty<string>();

    public bool AddAdmin(string org, string userLogin)
    {
        var config = Get(org);
        if (config is null)
            return false;
        lock (config.Admins)
            if (!config.Admins.Contains(userLogin))
                config.Admins.Add(userLogin);
        return true;
    }
}
