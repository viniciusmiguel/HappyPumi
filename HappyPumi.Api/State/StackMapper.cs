#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>Maps the <see cref="StoredStack"/> domain record to the <c>AppStack</c> wire DTOs.</summary>
public static class StackMapper
{
    public static AppStack ToAppStack(StoredStack stack) => new()
    {
        Id = stack.Coordinates.Qualified,
        OrgName = stack.Coordinates.Org,
        ProjectName = stack.Coordinates.Project,
        StackName = stack.Coordinates.Stack,
        Version = stack.Version,
        Tags = new Dictionary<string, string>(stack.Tags),
        Config = stack.Config,
        // No update is in flight until the lifecycle endpoints land (ENDPOINTS.md 1c).
        ActiveUpdate = string.Empty,
        CurrentOperation = null,
    };

    /// <summary>Lightweight summary for ListUserStacks. LastUpdate is the start time of the most recent update.</summary>
    public static AppStackSummary ToSummary(StoredStack stack) => new()
    {
        Id = stack.Coordinates.Qualified,
        OrgName = stack.Coordinates.Org,
        ProjectName = stack.Coordinates.Project,
        StackName = stack.Coordinates.Stack,
        LastUpdate = stack.History.Count > 0 ? stack.History[^1].Info.StartTime : null,
        ResourceCount = null,
        Links = null,
    };

    /// <summary>
    /// The richer per-version response (GetStackUpdate / GetLatestStackUpdate). Wraps the history entry's
    /// <c>AppUpdateInfo</c> as <c>Info</c> — the CLI's GetLatestConfiguration reads <c>Info.Config</c>.
    /// </summary>
    public static UpdateInfo ToUpdateInfo(StoredStack stack, StoredHistoryEntry entry) => new()
    {
        Info = entry.Info,
        Version = entry.Info.Version,
        UpdateId = entry.UpdateId,
        LatestVersion = stack.Version,
        RequestedBy = HappyPumiUser,
    };

    /// <summary>The single seeded identity (mirrors GetCurrentUser) attributed to updates for now.</summary>
    private static UserInfo HappyPumiUser => new()
    {
        GithubLogin = "happypumi",
        Name = "happypumi",
        AvatarUrl = "https://example.invalid/avatar.png",
    };
}
