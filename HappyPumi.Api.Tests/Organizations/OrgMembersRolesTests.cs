using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for Tier 3 (ENDPOINTS.md): org members, custom roles, team-role assignment, and the
/// audit-log listing. Endpoints are still anonymous (RBAC enforcement is a follow-up); this verifies the
/// data model and wire contracts. Each test uses a unique org since the store is shared.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class OrgMembersRolesTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task MemberAddListUpdateDelete()
    {
        using var client = app.CreateClient();
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
        using var client = app.CreateClient();
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
        using var client = app.CreateClient();
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
    public async Task AuditLogsAreAnEmptyPage()
    {
        using var client = app.CreateClient();

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
