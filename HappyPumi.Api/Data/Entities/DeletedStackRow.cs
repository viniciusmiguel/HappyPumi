#nullable enable

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A persisted deleted-stack tombstone (org-admin PR5, ADR-0005). Key: (Org, ProgramId). Recorded when a
/// stack is soft-deleted and removed when the stack is restored; scalar columns carry the fields the
/// restore-list endpoint returns.
/// </summary>
public sealed class DeletedStackRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string ProjectName { get; set; } = default!;
    public string StackName { get; set; } = default!;
    public string ProgramId { get; set; } = default!;
    public long Version { get; set; }
    public long DeletedAtUnix { get; set; }
}
