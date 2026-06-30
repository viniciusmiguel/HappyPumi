#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A persisted change-request timeline event (PR2, ADR-0005). Key: Id (a generated guid). Scoped by Org and
/// ChangeRequestId so the events endpoint can list a single change request's timeline oldest-first.
/// </summary>
public sealed class ChangeRequestEventRow
{
    public string Id { get; set; } = default!;
    public string ChangeRequestId { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string? Comment { get; set; }
    public long RevisionNumber { get; set; }
    public string CreatedBy { get; set; } = "happypumi";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
