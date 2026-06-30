#nullable enable

using System;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Factory for change-request timeline events (change-requests PR2). Centralises the boilerplate (new guid
/// id, org/CR scoping, actor, timestamp) so each endpoint only supplies the event-specific fields.
/// </summary>
internal static class ChangeRequestActivity
{
    /// <summary>Builds a timeline event for a change request, ready to <c>Append</c>.</summary>
    public static StoredChangeRequestEvent Event(StoredChangeRequest cr, string eventType, string actor,
        string? comment = null, long revisionNumber = 0) => new()
    {
        Id = Guid.NewGuid().ToString(),
        ChangeRequestId = cr.Id,
        Org = cr.Org,
        EventType = eventType,
        Comment = comment,
        RevisionNumber = revisionNumber,
        CreatedBy = actor,
        CreatedAt = DateTime.UtcNow,
    };
}
