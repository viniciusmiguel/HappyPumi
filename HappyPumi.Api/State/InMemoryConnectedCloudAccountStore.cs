#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IConnectedCloudAccountStore"/> (ADR-0005), keyed by (org, provider).</summary>
public sealed class InMemoryConnectedCloudAccountStore : IConnectedCloudAccountStore
{
    private readonly ConcurrentDictionary<(string Org, string Provider), StoredConnectedCloudAccount> _state = new();

    public void Upsert(string org, string provider, IReadOnlyList<CloudAccountEntry> accounts, string? credential)
        => _state[(org, provider)] = new StoredConnectedCloudAccount
        {
            Org = org,
            Provider = provider,
            Accounts = accounts.ToList(),
            Credential = credential,
        };

    public IReadOnlyList<CloudAccountEntry> List(string org, string provider)
        => _state.TryGetValue((org, provider), out var found) ? found.Accounts : Array.Empty<CloudAccountEntry>();
}
