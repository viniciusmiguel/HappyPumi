#nullable enable

using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// One entry in a stack's update history (what <c>pulumi stack history</c> lists). Holds the lean
/// <see cref="AppUpdateInfo"/> the history endpoints return, plus the update id used to build the richer
/// per-version response.
/// </summary>
public sealed class StoredHistoryEntry
{
    public required string UpdateId { get; init; }
    public required AppUpdateInfo Info { get; init; }
}
