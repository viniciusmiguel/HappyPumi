#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// A single entry in a change request's append-only activity timeline (change-requests PR2, ADR-0005).
/// <see cref="EventType"/> selects which optional fields are meaningful (e.g. <c>commented</c> →
/// <see cref="Comment"/>, <c>revision_added</c> → <see cref="RevisionNumber"/>).
/// </summary>
public sealed class StoredChangeRequestEvent
{
    public required string Id { get; init; }
    public required string ChangeRequestId { get; init; }
    public required string Org { get; init; }

    // status_changed|commented|approved_by_user|unapproved_by_user|revision_added|description_updated
    public required string EventType { get; set; }
    public string? Comment { get; set; }
    public long RevisionNumber { get; set; }
    public string CreatedBy { get; set; } = "happypumi";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persistence seam for the change-request timeline (ADR-0005). Append-only: events are never mutated or
/// deleted. <see cref="List"/> returns the timeline oldest-first for a single change request.
/// </summary>
public interface IChangeRequestEventStore
{
    /// <summary>Appends an event to a change request's timeline and returns it.</summary>
    StoredChangeRequestEvent Append(StoredChangeRequestEvent ev);

    /// <summary>The timeline for one change request, oldest-first.</summary>
    IReadOnlyList<StoredChangeRequestEvent> List(string org, string changeRequestId);
}
