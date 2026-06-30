#nullable enable

using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Translates the flat <see cref="StoredChangeRequest"/> persistence shape into the wire contracts
/// (change-requests PR2). Only the <c>environment</c> target discriminator exists in the generated
/// contracts, so the entity is always a <see cref="TargetEntityEnvironment"/>.
/// </summary>
internal static class ChangeRequestMapper
{
    /// <summary>Maps a stored change request to its <see cref="ChangeRequest"/> output contract.</summary>
    public static ChangeRequest ToContract(StoredChangeRequest c) => new()
    {
        Id = c.Id,
        OrgId = c.Org,
        Action = c.Action,
        Description = c.Description,
        Status = c.Status,
        LatestRevisionNumber = c.LatestRevisionNumber,
        CreatedAt = c.CreatedAt,
        CreatedBy = UserOf(c.CreatedBy),
        Entity = EntityOf(c),
    };

    /// <summary>Maps a stored change request plus its gate evaluation to the read response.</summary>
    public static GetChangeRequestResponse ToResponse(StoredChangeRequest c, ChangeRequestGateEvaluation evaluation) => new()
    {
        Id = c.Id,
        OrgId = c.Org,
        Action = c.Action,
        Description = c.Description,
        Status = c.Status,
        LatestRevisionNumber = c.LatestRevisionNumber,
        CreatedAt = c.CreatedAt,
        CreatedBy = UserOf(c.CreatedBy),
        Entity = EntityOf(c),
        GateEvaluation = evaluation,
    };

    private static TargetEntityEnvironment EntityOf(StoredChangeRequest c) => new()
    {
        EntityType = "environment", Project = c.TargetProject, Name = c.TargetEnv,
    };

    /// <summary>Builds the public <see cref="UserInfo"/> for a login (display name == login, no avatar).</summary>
    internal static UserInfo UserOf(string login) => new()
    {
        GithubLogin = login, Name = login, AvatarUrl = "",
    };
}
