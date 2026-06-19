#nullable enable

using System.Globalization;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>Maps stored environments to the ESC wire contracts.</summary>
internal static class EnvironmentMapper
{
    private static string Iso(System.DateTime t) => t.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    public static UserInfo Owner(StoredEnvironment e) => new()
    {
        GithubLogin = e.OwnerLogin, Name = e.OwnerName, AvatarUrl = "",
    };

    public static OrgEnvironment ToOrgEnvironment(StoredEnvironment e) => new()
    {
        Id = $"{e.Coordinates.Org}/{e.Coordinates.Project}/{e.Coordinates.Name}",
        Organization = e.Coordinates.Org, Project = e.Coordinates.Project, Name = e.Coordinates.Name,
        Created = Iso(e.Created), Modified = Iso(e.Modified), OwnedBy = Owner(e),
        Tags = e.Tags, Settings = new EnvironmentSettings { DeletionProtected = e.DeletionProtected },
    };
}
