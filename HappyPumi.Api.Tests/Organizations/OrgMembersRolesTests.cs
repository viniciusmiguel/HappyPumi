using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for Tier 3 (ENDPOINTS.md): org members, custom roles, team-role assignment, and the
/// audit-log listing. These endpoints require the org admin role (ADR-0007), so the tests authenticate;
/// the enforcement tests cover the unauthenticated (401) and non-admin (403) paths. Unique org per test.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class OrgMembersRolesTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task Returns401WhenUnauthenticated()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync($"/api/orgs/{NewOrg()}/members");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Returns403WhenNotAdmin()
    {
        // A non-admin caller (token convention role:member:bob) is authenticated but lacks the OrgAdmin role.
        using var client = app.CreateAuthedClient("role:member:bob");

        using var response = await client.GetAsync($"/api/orgs/{NewOrg()}/members");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MemberAddListUpdateDelete()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();

        using var add = await client.PostAsJsonAsync($"/api/orgs/{org}/members/alice",
            new Dictionary<string, string> { ["role"] = "admin" });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        var member = await add.Content.ReadFromJsonAsync<OrganizationMember>();
        Assert.Equal("admin", member!.Role);
        Assert.Equal("alice", member.User.GithubLogin);

        var list = await client.GetFromJsonAsync<ListOrganizationMembersResponse>($"/api/orgs/{org}/members");
        Assert.Single(list!.Members);

        using var update = await client.PatchAsJsonAsync($"/api/orgs/{org}/members/alice",
            new Dictionary<string, string> { ["role"] = "member" });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        using var delete = await client.DeleteAsync($"/api/orgs/{org}/members/alice");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using var deleteAgain = await client.DeleteAsync($"/api/orgs/{org}/members/alice");
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
    }

    [Fact]
    public async Task RoleCreateGetUpdateDelete()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();

        var created = await Post<PermissionDescriptorRecord>(client, $"/api/orgs/{org}/roles",
            new PermissionDescriptorBase { Name = "deployer", UxPurpose = "organization" });
        Assert.False(string.IsNullOrEmpty(created.Id));
        Assert.Equal(org, created.OrgId);

        var fetched = await client.GetFromJsonAsync<PermissionDescriptorRecord>($"/api/orgs/{org}/roles/{created.Id}");
        Assert.Equal("deployer", fetched!.Name);

        var roles = await client.GetFromJsonAsync<ListRolesResponse>($"/api/orgs/{org}/roles");
        Assert.Single(roles!.Roles);

        using var deleted = await client.DeleteAsync($"/api/orgs/{org}/roles/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var missing = await client.GetAsync($"/api/orgs/{org}/roles/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task TeamRoleAssignmentRequiresAnExistingRole()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();
        var role = await Post<PermissionDescriptorRecord>(client, $"/api/orgs/{org}/roles",
            new PermissionDescriptorBase { Name = "deployer" });

        // Assignment carries no body; send an empty JSON object (as a client would) so binding is happy.
        var empty = new Dictionary<string, string>();
        using var bad = await client.PostAsJsonAsync($"/api/orgs/{org}/teams/platform/roles/missing", empty);
        Assert.Equal(HttpStatusCode.NotFound, bad.StatusCode);

        using var ok = await client.PostAsJsonAsync($"/api/orgs/{org}/teams/platform/roles/{role.Id}", empty);
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);

        using var remove = await client.DeleteAsync($"/api/orgs/{org}/teams/platform/roles/{role.Id}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
    }

    [Fact]
    public async Task PulumiTeamCreateListGetWithRoleAssignment()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();

        // Create a Pulumi-managed team.
        var created = await Post<Team>(client, $"/api/orgs/{org}/teams/pulumi",
            new { name = "platform-eng", displayName = "Platform Engineering", description = "Owns platform stacks" });
        Assert.Equal("platform-eng", created.Name);
        Assert.Equal("Platform Engineering", created.DisplayName);
        Assert.Equal("pulumi", created.Kind);

        // A duplicate name conflicts.
        using var dup = await client.PostAsJsonAsync($"/api/orgs/{org}/teams/pulumi", new { name = "platform-eng" });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        // It appears in the org's team list.
        var list = await client.GetFromJsonAsync<ListTeamsResponse>($"/api/orgs/{org}/teams");
        Assert.Contains(list!.Teams, t => t.Name == "platform-eng");

        // Assigning a role surfaces on the team's roleIds.
        var role = await Post<PermissionDescriptorRecord>(client, $"/api/orgs/{org}/roles",
            new PermissionDescriptorBase { Name = "deployer" });
        using var assign = await client.PostAsJsonAsync(
            $"/api/orgs/{org}/teams/platform-eng/roles/{role.Id}", new Dictionary<string, string>());
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

        var team = await client.GetFromJsonAsync<Team>($"/api/orgs/{org}/teams/platform-eng");
        Assert.Contains(role.Id, team!.RoleIds!);
    }

    [Fact]
    public async Task GetUnknownTeamIs404()
    {
        using var client = app.CreateAuthedClient();
        using var resp = await client.GetAsync($"/api/orgs/{NewOrg()}/teams/nope");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task AuditLogsAreAnEmptyPage()
    {
        using var client = app.CreateAuthedClient();

        var logs = await client.GetFromJsonAsync<ResponseAuditLogs>($"/api/orgs/{NewOrg()}/auditlogs");

        Assert.NotNull(logs);
        Assert.Empty(logs!.AuditLogEvents);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
