#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IChangeRequestStore"/> (ADR-0005), keyed by (org, id). Used by unit tests.</summary>
public sealed class InMemoryChangeRequestStore : IChangeRequestStore
{
    private readonly ConcurrentDictionary<(string Org, string Id), StoredChangeRequest> _requests = new();

    public StoredChangeRequest Create(StoredChangeRequest cr)
    {
        _requests[(cr.Org, cr.Id)] = cr;
        return cr;
    }

    public IReadOnlyList<StoredChangeRequest> List(string org)
        => _requests.Values.Where(c => c.Org == org).OrderByDescending(c => c.CreatedAt).ToArray();

    public StoredChangeRequest? Get(string org, string id)
        => _requests.TryGetValue((org, id), out var cr) ? cr : null;

    public StoredChangeRequest? Update(string org, string id, Action<StoredChangeRequest> mutate)
    {
        if (!_requests.TryGetValue((org, id), out var cr))
            return null;
        mutate(cr);
        return cr;
    }
}
