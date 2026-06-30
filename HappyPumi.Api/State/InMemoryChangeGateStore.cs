#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IChangeGateStore"/> (ADR-0005), keyed by (org, id). Used by unit tests.</summary>
public sealed class InMemoryChangeGateStore : IChangeGateStore
{
    private readonly ConcurrentDictionary<(string Org, string Id), StoredChangeGate> _gates = new();

    public StoredChangeGate Create(StoredChangeGate gate)
    {
        _gates[(gate.Org, gate.Id)] = gate;
        return gate;
    }

    public IReadOnlyList<StoredChangeGate> List(string org)
        => _gates.Values.Where(g => g.Org == org).OrderByDescending(g => g.Created).ToArray();

    public StoredChangeGate? Get(string org, string id)
        => _gates.TryGetValue((org, id), out var gate) ? gate : null;

    public StoredChangeGate? Update(string org, string id, Action<StoredChangeGate> mutate)
    {
        if (!_gates.TryGetValue((org, id), out var gate))
            return null;
        mutate(gate);
        return gate;
    }

    public bool Delete(string org, string id) => _gates.TryRemove((org, id), out _);
}
