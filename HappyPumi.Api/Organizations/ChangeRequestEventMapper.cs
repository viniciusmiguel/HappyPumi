#nullable enable

using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Maps a stored timeline event to the correct polymorphic <see cref="ChangeRequestEvent"/> subtype by its
/// <see cref="StoredChangeRequestEvent.EventType"/> discriminator (change-requests PR2).
/// </summary>
internal static class ChangeRequestEventMapper
{
    /// <summary>Builds the polymorphic event contract for a stored event.</summary>
    public static ChangeRequestEvent ToContract(StoredChangeRequestEvent e)
    {
        var ev = Instantiate(e.EventType);
        ev.Id = e.Id;
        ev.ChangeRequestId = e.ChangeRequestId;
        ev.EventType = e.EventType;
        ev.Comment = e.Comment;
        ev.RevisionNumber = e.RevisionNumber;
        ev.CreatedAt = e.CreatedAt;
        ev.CreatedBy = new UserInfo { GithubLogin = e.CreatedBy, Name = e.CreatedBy, AvatarUrl = "" };
        return ev;
    }

    private static ChangeRequestEvent Instantiate(string eventType) => eventType switch
    {
        "approved_by_user" => new ChangeRequestApprovedEvent(),
        "commented" => new ChangeRequestCommentedEvent(),
        "description_updated" => new ChangeRequestDescriptionUpdatedEvent(),
        "revision_added" => new ChangeRequestRevisionAddedEvent(),
        "status_changed" => new ChangeRequestStatusChangedEvent(),
        "unapproved_by_user" => new ChangeRequestUnapprovedEvent(),
        _ => new ChangeRequestEvent(),
    };
}
