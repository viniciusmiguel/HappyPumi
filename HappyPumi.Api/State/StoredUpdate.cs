#nullable enable

using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// One update operation against a stack (the <c>update</c>/<c>preview</c>/<c>refresh</c>/<c>destroy</c>
/// lifecycle of ENDPOINTS.md 1c/1d). Held by <see cref="IUpdateStore"/> and driven through its states
/// by <see cref="UpdateLifecycle"/>.
/// </summary>
public sealed class StoredUpdate
{
    public required string UpdateId { get; init; }
    public required StackCoordinates Coordinates { get; init; }

    /// <summary>The update kind / route segment: update, preview, refresh, or destroy.</summary>
    public required string Kind { get; init; }

    /// <summary>True for preview: its checkpoints are never promoted to the stack (it is a dry run).</summary>
    public required bool DryRun { get; init; }

    public string Status { get; set; } = UpdateStatuses.NotStarted;

    /// <summary>Lease token handed to the CLI by StartUpdate and echoed on subsequent operations.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>The stack version this update will produce once completed.</summary>
    public long Version { get; set; }

    /// <summary>Latest checkpoint PATCHed during the update; promoted to the stack on success.</summary>
    public AppUntypedDeployment? Checkpoint { get; set; }
}
