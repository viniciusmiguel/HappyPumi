using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for the stack access surface (PR4): collaborators (list/delete), stack teams
/// (list/update), and the per-member stack-permission lookup. The stack creator is auto-recorded as an
/// admin collaborator, so an authenticated client (login <c>happypumi</c>) seeds real collaborator data.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackAccessTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";
    private const string Creator = "happypumi";

    private static string Base(string s) => $"/api/stacks/{Org}/{Project}/{s}";

    private static async Task<string> NewStack(HttpClient client)
    {
        var stack = $"acc-{Guid.NewGuid():N}";
        var r = await client.PostAsJsonAsync($"/api/stacks/{Org}/{Project}", new AppCreateStackRequest { StackName = stack });
        r.EnsureSuccessStatusCode();
        return stack;
    }

    private static async Task<string> NewTeam(HttpClient client)
    {
        var team = $"team-{Guid.NewGuid():N}";
        var r = await client.PostAsJsonAsync($"/api/orgs/{Org}/teams/pulumi",
            new { name = team, displayName = "Engineering", description = "Eng team" });
        r.EnsureSuccessStatusCode();
        return team;
    }

    // ── Collaborators ──────────────────────────────────────────────────────────
    [Fact]
    public async Task ListCollaboratorsReturnsTheCreatorAsAdmin()
    {
        var client = app.CreateAuthedClient();
        var stack = await NewStack(client);

        var resp = await client.GetFromJsonAsync<ListStackCollaboratorsResponse>($"{Base(stack)}/collaborators");

        Assert.NotNull(resp);
        Assert.Equal(Creator, resp!.StackCreatorUserName);
        var user = Assert.Single(resp.Users);
        Assert.Equal(Creator, user.User.GithubLogin);
        Assert.Equal(103, user.Permission);
    }

    [Fact]
    public async Task ListCollaboratorsUnknownStackReturns404()
    {
        var client = app.CreateAuthedClient();
        using var r = await client.GetAsync($"{Base("missing-" + Guid.NewGuid().ToString("N"))}/collaborators");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task DeleteCollaboratorRemovesThenReturns404()
    {
        var client = app.CreateAuthedClient();
        var stack = await NewStack(client);

        using var first = await client.DeleteAsync($"{Base(stack)}/collaborators/{Creator}");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        using var second = await client.DeleteAsync($"{Base(stack)}/collaborators/{Creator}");
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);

        var resp = await client.GetFromJsonAsync<ListStackCollaboratorsResponse>($"{Base(stack)}/collaborators");
        Assert.Empty(resp!.Users);
    }

    [Fact]
    public async Task DeleteCollaboratorUnknownStackReturns404()
    {
        var client = app.CreateAuthedClient();
        using var r = await client.DeleteAsync($"{Base("missing-" + Guid.NewGuid().ToString("N"))}/collaborators/{Creator}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ── Teams ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateTeamPermissionThenListTeamsReturnsIt()
    {
        var client = app.CreateAuthedClient();
        var stack = await NewStack(client);
        var team = await NewTeam(client);

        using var patch = await client.PatchAsJsonAsync(
            $"/api/console/stacks/{Org}/{Project}/{stack}/teams/{team}", new { permissions = 102 });
        Assert.Equal(HttpStatusCode.NoContent, patch.StatusCode);

        var resp = await client.GetFromJsonAsync<ListTeamsByStackResponse>($"{Base(stack)}/teams");
        Assert.Equal(Project, resp!.ProjectName);
        var stackTeam = Assert.Single(resp.Teams);
        Assert.Equal(team, stackTeam.Name);
        Assert.Equal(102, stackTeam.Permission);
        Assert.Equal("Engineering", stackTeam.DisplayName);
    }

    [Fact]
    public async Task UpdateTeamPermissionWithNullRemovesTheGrant()
    {
        var client = app.CreateAuthedClient();
        var stack = await NewStack(client);
        var team = await NewTeam(client);

        await client.PatchAsJsonAsync($"/api/console/stacks/{Org}/{Project}/{stack}/teams/{team}", new { permissions = 101 });
        using var remove = await client.PatchAsJsonAsync(
            $"/api/console/stacks/{Org}/{Project}/{stack}/teams/{team}", new { permissions = (long?)null });
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        var resp = await client.GetFromJsonAsync<ListTeamsByStackResponse>($"{Base(stack)}/teams");
        Assert.Empty(resp!.Teams);
    }

    [Fact]
    public async Task UpdateTeamPermissionUnknownTeamReturns404()
    {
        var client = app.CreateAuthedClient();
        var stack = await NewStack(client);

        using var r = await client.PatchAsJsonAsync(
            $"/api/console/stacks/{Org}/{Project}/{stack}/teams/ghost-team", new { permissions = 101 });
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task UpdateTeamPermissionUnknownStackReturns404()
    {
        var client = app.CreateAuthedClient();
        var team = await NewTeam(client);
        var missing = "missing-" + Guid.NewGuid().ToString("N");

        using var r = await client.PatchAsJsonAsync(
            $"/api/console/stacks/{Org}/{Project}/{missing}/teams/{team}", new { permissions = 101 });
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task ListTeamsUnknownStackReturns404()
    {
        var client = app.CreateAuthedClient();
        using var r = await client.GetAsync($"{Base("missing-" + Guid.NewGuid().ToString("N"))}/teams");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // ── Member stack permissions ─────────────────────────────────────────────────
    [Fact]
    public async Task MemberStackPermissionsReportsExplicitGrant()
    {
        var client = app.CreateAuthedClient();
        var stack = await NewStack(client);

        var resp = await client.GetFromJsonAsync<ListMemberStackPermissionsResponse>(
            $"/api/console/orgs/{Org}/members/{Creator}/stacks/{Project}/{stack}");

        Assert.NotNull(resp);
        Assert.Equal(103, resp!.ExplicitPermission);
        Assert.Equal(101, resp.OrganizationDefault);
        Assert.Equal(Creator, resp.UserInfo.GithubLogin);
        Assert.Empty(resp.TeamPermissions);
    }

    [Fact]
    public async Task MemberStackPermissionsUnknownUserReturns404()
    {
        var client = app.CreateAuthedClient();
        var stack = await NewStack(client);

        using var r = await client.GetAsync(
            $"/api/console/orgs/{Org}/members/nobody-{Guid.NewGuid():N}/stacks/{Project}/{stack}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task MemberStackPermissionsUnknownStackReturns404()
    {
        var client = app.CreateAuthedClient();
        using var r = await client.GetAsync(
            $"/api/console/orgs/{Org}/members/{Creator}/stacks/{Project}/missing-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }
}
