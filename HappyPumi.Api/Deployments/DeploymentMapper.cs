#nullable enable

using System.Globalization;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Deployments;

/// <summary>Maps a stored deployment to the console wire contracts (list snapshot + detail response).</summary>
internal static class DeploymentMapper
{
    // RFC3339 strings — the console parses deployment timestamps as ISO dates (not unix seconds).
    private static string Iso(System.DateTime t) => t.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static UserInfo Requester(StoredDeployment d) => new()
    {
        GithubLogin = d.RequestedByLogin ?? "unknown",
        Name = d.RequestedByName ?? d.RequestedByLogin ?? "unknown",
        AvatarUrl = "",
    };

    public static ListDeploymentSnapshot ToSnapshot(StoredDeployment d) => new()
    {
        Id = d.Id, Version = d.Version, Status = d.Status,
        Created = Iso(d.Created), Modified = Iso(d.Modified),
        RequestedBy = Requester(d), Initiator = d.RequestedByLogin,
        PulumiOperation = d.Operation, ProjectName = d.Project, StackName = d.Stack,
        Jobs = d.Jobs, Updates = d.Updates,
    };

    public static GetDeploymentResponse ToResponse(StoredDeployment d) => new()
    {
        Id = d.Id, Version = d.Version, LatestVersion = d.Version, Status = d.Status,
        Created = Iso(d.Created), Modified = Iso(d.Modified),
        RequestedBy = Requester(d), Initiator = d.RequestedByLogin,
        PulumiOperation = d.Operation, Jobs = d.Jobs,
    };
}
