#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IUpdateStore"/> keyed by update id (ADR-0005).</summary>
public sealed class InMemoryUpdateStore : IUpdateStore
{
    private readonly ConcurrentDictionary<string, StoredUpdate> _updates = new();

    public StoredUpdate Create(StackCoordinates stack, string kind, bool dryRun)
    {
        var update = new StoredUpdate
        {
            UpdateId = Guid.NewGuid().ToString(),
            Coordinates = stack,
            Kind = kind,
            DryRun = dryRun,
        };
        _updates[update.UpdateId] = update;
        return update;
    }

    public StoredUpdate? Find(string updateId)
        => _updates.TryGetValue(updateId, out var update) ? update : null;

    public StoredUpdate? FindByVersion(StackCoordinates stack, long version)
        => _updates.Values.FirstOrDefault(u =>
            u.Coordinates == stack && u.Version == version && !u.DryRun);

    // The stored record is the live object, so mutations are already visible; nothing to persist.
    public void Save(StoredUpdate update) { }

    public IReadOnlyList<StoredUpdate> ListByStack(StackCoordinates stack)
        => _updates.Values.Where(u => u.Coordinates == stack).ToList();

    public void AppendEvents(string updateId, IReadOnlyList<AppEngineEvent> events)
    {
        if (_updates.TryGetValue(updateId, out var update))
            update.Events.AddRange(events);
    }

    public IReadOnlyList<AppEngineEvent> GetEvents(string updateId)
        => _updates.TryGetValue(updateId, out var update) ? update.Events : new List<AppEngineEvent>();
}
