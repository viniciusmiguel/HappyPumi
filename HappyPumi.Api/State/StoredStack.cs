#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// The server-side state of a single stack held by <see cref="IStackStore"/>. This is the domain
/// record (distinct from the <c>AppStack</c> wire DTO) so storage is not coupled to the HTTP contract.
/// Resource checkpoints and the update history attach here as later tiers land (ENDPOINTS.md 1b/1c).
/// </summary>
public sealed class StoredStack
{
    public required StackCoordinates Coordinates { get; init; }

    /// <summary>Monotonic version, bumped once per completed update. Starts at 0 (never updated).</summary>
    public long Version { get; set; }

    public Dictionary<string, string> Tags { get; init; } = new();

    /// <summary>Service-managed config (secrets provider / ESC env), or null when none is set.</summary>
    public AppStackConfig? Config { get; set; }

    /// <summary>
    /// The current state checkpoint (the exported deployment), or null when the stack has never been
    /// deployed. Set by a completed update or a state import; read by ExportStack.
    /// </summary>
    public AppUntypedDeployment? Deployment { get; set; }

    /// <summary>Completed updates in chronological order (oldest first); the history endpoints read this.</summary>
    public List<StoredHistoryEntry> History { get; } = new();
}
