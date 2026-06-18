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
}
