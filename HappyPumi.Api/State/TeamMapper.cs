#nullable enable

using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>Maps a stored team to the <see cref="Team"/> wire DTO the console reads.</summary>
public static class TeamMapper
{
    public static Team ToTeam(StoredTeam t) => new()
    {
        Name = t.Name,
        DisplayName = t.DisplayName,
        Description = t.Description,
        Kind = t.Kind,
        Members = t.Members
            .Select(login => new TeamMemberInfo { GithubLogin = login, Name = login, Role = "member" })
            .ToList(),
        RoleIds = t.RoleIds.ToList(),
    };
}
