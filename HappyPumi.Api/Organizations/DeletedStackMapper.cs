#nullable enable

using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>Maps a <see cref="StoredDeletedStack"/> tombstone to the <see cref="DeletedStack"/> wire shape.</summary>
internal static class DeletedStackMapper
{
    public static DeletedStack ToContract(StoredDeletedStack t) => new()
    {
        Id = t.Id,
        ProgramId = t.ProgramId,
        ProjectName = t.ProjectName,
        StackName = t.StackName,
        Version = t.Version,
        DeletedAt = t.DeletedAtUnix,
        // No per-update summary is retained on delete; surface a minimal empty summary (never null).
        LastUpdate = new UpdateSummary(),
    };
}
