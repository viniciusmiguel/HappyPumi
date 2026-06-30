#nullable enable

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for update records (ADR-0005), in-memory by default like <see cref="IStackStore"/>.
/// Records are mutated in place through <see cref="UpdateLifecycle"/>; this store only owns their identity
/// and lookup. Safe for concurrent use.
/// </summary>
public interface IUpdateStore
{
    /// <summary>Creates and stores a new update for the stack, returning the record with a fresh id.</summary>
    StoredUpdate Create(StackCoordinates stack, string kind, bool dryRun);

    /// <summary>Returns the update by id, or null when it does not exist.</summary>
    StoredUpdate? Find(string updateId);

    /// <summary>
    /// Returns the succeeded (non-dry-run) update at the given stack version, or null. Used to recover the
    /// resource checkpoint for a historical version (per-version resource views).
    /// </summary>
    StoredUpdate? FindByVersion(StackCoordinates stack, long version);

    /// <summary>
    /// Persists mutations made to a record obtained from <see cref="Find"/> or <see cref="Create"/>. A
    /// no-op for in-memory (the record is live); writes the row for durable stores.
    /// </summary>
    void Save(StoredUpdate update);
}
