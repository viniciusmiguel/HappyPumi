#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IDeletedStackStore"/> (ADR-0005), keyed by (org, programId). Used by unit tests.</summary>
public sealed class InMemoryDeletedStackStore : IDeletedStackStore
{
    private readonly ConcurrentDictionary<(string Org, string ProgramId), StoredDeletedStack> _tombstones = new();

    public StoredDeletedStack Record(StoredDeletedStack deleted)
    {
        _tombstones[(deleted.Org, deleted.ProgramId)] = deleted;
        return deleted;
    }

    public IReadOnlyList<StoredDeletedStack> List(string org)
        => _tombstones.Values.Where(t => t.Org == org).OrderByDescending(t => t.DeletedAtUnix).ToArray();

    public StoredDeletedStack? Get(string org, string programId)
        => _tombstones.TryGetValue((org, programId), out var tombstone) ? tombstone : null;

    public bool Remove(string org, string programId) => _tombstones.TryRemove((org, programId), out _);
}
