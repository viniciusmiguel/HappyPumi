#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Stacks;

/// <summary>
/// Builds the update-timeline and preview-history projections shared by the timeline/preview endpoints.
/// Previews are the stack's dry-run lifecycle records (they are never recorded in stack history).
/// </summary>
internal static class StackTimeline
{
    /// <summary>The timeline culminating in <paramref name="focal"/>: the focal update plus the stack's previews.</summary>
    public static GetUpdateTimelineResponse For(IUpdateStore updates, StoredStack stack, StoredHistoryEntry focal) => new()
    {
        Update = UpdateInfoMapper.FromHistory(focal, stack.Version),
        Previews = Previews(updates, stack),
        CollatedUpdateEvents = new List<UpdateInfo>(),
        CollatedPullRequest = null,
    };

    /// <summary>The stack's dry-run previews, newest first, projected to <see cref="UpdateInfo"/>.</summary>
    public static List<UpdateInfo> Previews(IUpdateStore updates, StoredStack stack)
        => updates.ListByStack(stack.Coordinates)
            .Where(u => u.DryRun)
            .OrderByDescending(u => u.StartedAt)
            .Select(u => UpdateInfoMapper.FromPreview(u, stack.Version))
            .ToList();
}
