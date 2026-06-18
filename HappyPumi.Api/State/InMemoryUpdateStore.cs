#nullable enable

using System;
using System.Collections.Concurrent;

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
}
