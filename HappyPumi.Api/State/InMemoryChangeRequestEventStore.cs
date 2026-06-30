#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>
/// In-memory <see cref="IChangeRequestEventStore"/> (ADR-0005), keyed by (org, changeRequestId) → ordered
/// list. Append order is preserved so <see cref="List"/> returns the timeline oldest-first. Used by tests.
/// </summary>
public sealed class InMemoryChangeRequestEventStore : IChangeRequestEventStore
{
    private readonly ConcurrentDictionary<(string Org, string Cr), List<StoredChangeRequestEvent>> _events = new();

    public StoredChangeRequestEvent Append(StoredChangeRequestEvent ev)
    {
        var list = _events.GetOrAdd((ev.Org, ev.ChangeRequestId), _ => new List<StoredChangeRequestEvent>());
        lock (list)
            list.Add(ev);
        return ev;
    }

    public IReadOnlyList<StoredChangeRequestEvent> List(string org, string changeRequestId)
    {
        if (!_events.TryGetValue((org, changeRequestId), out var list))
            return Array.Empty<StoredChangeRequestEvent>();
        lock (list)
            return list.OrderBy(e => e.CreatedAt).ThenBy(e => e.Id).ToArray();
    }
}
