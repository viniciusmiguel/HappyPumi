#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for stack state (ADR-0005). The default implementation is in-memory so the IaC
/// workflow runs offline in tests and local dev; a PostgreSQL-backed implementation can be dropped in
/// behind this interface without touching the endpoints. All operations are safe for concurrent use.
/// </summary>
public interface IStackStore
{
    /// <summary>True when at least one stack exists under <paramref name="org"/>/<paramref name="project"/>.</summary>
    bool ProjectExists(string org, string project);

    /// <summary>Returns the stored stack, or null when it does not exist.</summary>
    StoredStack? Find(StackCoordinates coordinates);

    /// <summary>Creates the stack. Returns false (creating nothing) when one already exists at those coordinates.</summary>
    bool TryCreate(StoredStack stack);

    /// <summary>Removes the stack. Returns false when no stack exists at those coordinates.</summary>
    bool Delete(StackCoordinates coordinates);

    /// <summary>Replaces the stack's service-managed config. Returns the updated stack, or null when it does not exist.</summary>
    StoredStack? SetConfig(StackCoordinates coordinates, AppStackConfig config);

    /// <summary>Clears the stack's service-managed config. Returns false when the stack does not exist.</summary>
    bool ClearConfig(StackCoordinates coordinates);

    /// <summary>
    /// Replaces the stack's state checkpoint and bumps its version when <paramref name="bumpVersion"/>
    /// is set (a completed update bumps; a raw state import does not). Returns the updated stack, or
    /// null when it does not exist.
    /// </summary>
    StoredStack? SetDeployment(StackCoordinates coordinates, AppUntypedDeployment deployment, bool bumpVersion);

    /// <summary>All stored stacks (used by ListUserStacks). Order is unspecified.</summary>
    IReadOnlyCollection<StoredStack> All();

    /// <summary>Appends a completed update to the stack's history. Returns false when the stack is unknown.</summary>
    bool RecordHistory(StackCoordinates coordinates, StoredHistoryEntry entry);

    /// <summary>Sets (or overwrites) a single tag. Returns the updated stack, or null when it does not exist.</summary>
    StoredStack? SetTag(StackCoordinates coordinates, string name, string value);

    /// <summary>Replaces the stack's entire tag set. Returns the updated stack, or null when it does not exist.</summary>
    StoredStack? ReplaceTags(StackCoordinates coordinates, IReadOnlyDictionary<string, string> tags);

    /// <summary>
    /// Moves a stack to new coordinates (project and/or stack name). Returns the moved stack, null when
    /// the source is missing, or throws nothing — a name collision is reported by <paramref name="collision"/>.
    /// </summary>
    StoredStack? Rename(StackCoordinates from, StackCoordinates to, out bool collision);
}
