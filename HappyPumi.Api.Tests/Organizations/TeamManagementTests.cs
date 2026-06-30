using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the Settings-cluster PR2 team management endpoints (update / delete / list-roles /
/// list-teams-with-role / enable-team-roles). They run against the real Postgres-backed identity store and
/// drive the full lifecycle: create a team via the implemented endpoint, then exercise each new route.
/// Unique org per test so they stay independent.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class TeamManagementTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task TeamLifecycleUpdateRolesAndDelete()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();
        var role = await Post<PermissionDescriptorRecord>(client, $"/api/orgs/{org}/roles",
            new PermissionDescriptorBase { Name = "deployer" });
        await Post<Team>(client, $"/api/orgs/{org}/teams/pulumi",
            new { name = "platform", displayName = "Platform", description = "old" });

        // Update display name + description (the rename path is covered separately).
        using var update = await client.PatchAsJsonAsync($"/api/orgs/{org}/teams/platform",
            new { newDisplayName = "Platform Eng", newDescription = "new" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<Team>();
        Assert.Equal("Platform Eng", updated!.DisplayName);
        Assert.Equal("new", updated.Description);

        // Enable team roles assigns the org's only role and returns it.
        var enabled = await Post<PermissionDescriptorRecord>(client,
            $"/api/orgs/{org}/teams/platform/enable-team-roles", new Dictionary<string, string>());
        Assert.Equal(role.Id, enabled.Id);

        // The role now shows up on the team's role list...
        var teamRoles = await client.GetFromJsonAsync<ListTeamRolesResponse>($"/api/orgs/{org}/teams/platform/roles");
        Assert.Contains(teamRoles!.Roles, r => r.Id == role.Id);

        // ...and the team shows up under the role's teams.
        var withRole = await client.GetFromJsonAsync<ListTeamsWithRoleResponse>($"/api/orgs/{org}/roles/{role.Id}/teams");
        Assert.Contains(withRole!.Teams, t => t.Name == "platform");

        using var deleted = await client.DeleteAsync($"/api/orgs/{org}/teams/platform");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var deletedAgain = await client.DeleteAsync($"/api/orgs/{org}/teams/platform");
        Assert.Equal(HttpStatusCode.NotFound, deletedAgain.StatusCode);
    }

    [Fact]
    public async Task UpdateTeamRenameCarriesRoleGrant()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();
        var role = await Post<PermissionDescriptorRecord>(client, $"/api/orgs/{org}/roles",
            new PermissionDescriptorBase { Name = "deployer" });
        await Post<Team>(client, $"/api/orgs/{org}/teams/pulumi", new { name = "platform" });
        await Post<PermissionDescriptorRecord>(client, $"/api/orgs/{org}/teams/platform/enable-team-roles",
            new Dictionary<string, string>());

        using var renamed = await client.PatchAsJsonAsync($"/api/orgs/{org}/teams/platform", new { newName = "infra" });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var team = await renamed.Content.ReadFromJsonAsync<Team>();
        Assert.Equal("infra", team!.Name);

        using var oldGone = await client.GetAsync($"/api/orgs/{org}/teams/platform");
        Assert.Equal(HttpStatusCode.NotFound, oldGone.StatusCode);

        // The role grant followed the rename.
        var withRole = await client.GetFromJsonAsync<ListTeamsWithRoleResponse>($"/api/orgs/{org}/roles/{role.Id}/teams");
        Assert.Contains(withRole!.Teams, t => t.Name == "infra");
    }

    [Fact]
    public async Task MissingTeamReturns404()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();

        using var update = await client.PatchAsJsonAsync($"/api/orgs/{org}/teams/ghost", new { newDisplayName = "x" });
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);

        using var roles = await client.GetAsync($"/api/orgs/{org}/teams/ghost/roles");
        Assert.Equal(HttpStatusCode.NotFound, roles.StatusCode);

        using var enable = await client.PostAsJsonAsync($"/api/orgs/{org}/teams/ghost/enable-team-roles",
            new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.NotFound, enable.StatusCode);
    }

    [Fact]
    public async Task EnableTeamRolesWithoutAnyOrgRoleIs400()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();
        await Post<Team>(client, $"/api/orgs/{org}/teams/pulumi", new { name = "platform" });

        using var enable = await client.PostAsJsonAsync($"/api/orgs/{org}/teams/platform/enable-team-roles",
            new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.BadRequest, enable.StatusCode);
    }

    [Fact]
    public async Task ListTeamsWithRoleIsEmptyForUnusedRole()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();

        var teams = await client.GetFromJsonAsync<ListTeamsWithRoleResponse>(
            $"/api/orgs/{org}/roles/{Guid.NewGuid():N}/teams");

        Assert.NotNull(teams);
        Assert.Empty(teams!.Teams);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
