#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// A tombstone recorded when a stack is soft-deleted (org-admin PR5, ADR-0005). It carries the coordinates
/// and version needed to surface the stack on the restore page and to recreate it on restore. Keyed by
/// (<see cref="Org"/>, <see cref="ProgramId"/>) — the restore route addresses a tombstone by its program id.
/// </summary>
public sealed class StoredDeletedStack
{
    public required string Id { get; init; }          // opaque row id (guid)
    public required string Org { get; init; }
    public required string ProjectName { get; init; }
    public required string StackName { get; init; }
    public required string ProgramId { get; init; }   // stable id used by the restore route
    public long Version { get; set; }
    public long DeletedAtUnix { get; set; }
}

/// <summary>
/// Persistence seam for deleted-stack tombstones (ADR-0005). Backed by PostgreSQL in production and an
/// in-memory map in unit tests. Tombstones are keyed by program id within an org.
/// </summary>
public interface IDeletedStackStore
{
    /// <summary>Persists a tombstone and returns it.</summary>
    StoredDeletedStack Record(StoredDeletedStack deleted);

    /// <summary>All tombstones for an org, newest deletion first.</summary>
    IReadOnlyList<StoredDeletedStack> List(string org);

    /// <summary>A single tombstone by program id within an org, or null when missing.</summary>
    StoredDeletedStack? Get(string org, string programId);

    /// <summary>Removes a tombstone (on restore). False when it does not exist.</summary>
    bool Remove(string org, string programId);
}
