#nullable enable

using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Maps stored stack access grants to the collaborator/team wire DTOs shared by the access endpoints
/// (ListStackPermissions, ListStackTeams, ListMemberStackPermissions).
/// </summary>
public static class StackPermissionMapper
{
    private const string AvatarUrl = "https://example.invalid/avatar.png";

    public static UserInfo ToUserInfo(string login)
        => new() { GithubLogin = login, Name = login, AvatarUrl = AvatarUrl };

    public static UserPermission ToUserPermission(string login, long permission)
        => new() { Permission = permission, User = ToUserInfo(login) };

    /// <summary>Maps a team grant, enriching it from the org team (display/description/membership) when known.</summary>
    public static StackTeam ToStackTeam(string teamName, long permission, StoredTeam? team, string? caller)
        => new()
        {
            Name = teamName,
            Permission = permission,
            DisplayName = team?.DisplayName ?? teamName,
            Description = team?.Description ?? string.Empty,
            IsMember = caller is not null && team is not null && team.Members.Contains(caller),
        };
}
