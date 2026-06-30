#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Stacks;

/// <summary>
/// Projects stored update records into the console-facing <see cref="UpdateInfo"/> shape used by the
/// timeline and preview endpoints. History entries already carry an <see cref="AppUpdateInfo"/>; previews
/// (dry runs, never recorded in history) are synthesised from their lifecycle record.
/// </summary>
internal static class UpdateInfoMapper
{
    /// <summary>Wraps a completed-update history entry (e.g. the timeline's focal update).</summary>
    public static UpdateInfo FromHistory(StoredHistoryEntry entry, long latestVersion) => new()
    {
        Info = entry.Info,
        UpdateId = entry.UpdateId,
        Version = entry.Info.Version,
        LatestVersion = latestVersion,
        RequestedBy = Actor(entry.RequestedByLogin, entry.RequestedByName),
    };

    /// <summary>Synthesises an <see cref="UpdateInfo"/> for a dry-run preview lifecycle record.</summary>
    public static UpdateInfo FromPreview(StoredUpdate update, long latestVersion) => new()
    {
        // Previews record no distinct end time, so StartTime doubles as EndTime.
        Info = new AppUpdateInfo
        {
            Kind = update.Kind,
            Result = update.Status,
            Message = update.Message,
            StartTime = update.StartedAt,
            EndTime = update.StartedAt,
            Version = update.Version,
            Config = update.Config ?? new Dictionary<string, AppConfigValue>(),
            Environment = new Dictionary<string, string>(),
        },
        UpdateId = update.UpdateId,
        Version = update.Version,
        LatestVersion = latestVersion,
        RequestedBy = Actor(update.RequestedByLogin, update.RequestedByName),
    };

    private static UserInfo Actor(string? login, string? name) => new()
    {
        GithubLogin = login ?? string.Empty,
        Name = name ?? login ?? string.Empty,
        AvatarUrl = string.Empty,
    };
}
