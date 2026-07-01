using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the org core/members/roles endpoints (org-admin PR1) against real Postgres:
/// GetOrganization, UpdateOrganizationSettings (persisted), SetSoleOrganizationAdmin, ListUsersWithRole,
/// ListAvailableScopes and UpdateOrganizationDefaultRole. Members/roles are seeded through the real
/// <see cref="IIdentityStore"/> resolved from a request scope; unique org per test for independence.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class OrgSettingsTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task GetOrganizationReturnsSyntheticOrg()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        var result = await client.GetFromJsonAsync<Organization>($"/api/orgs/{org}");

        Assert.NotNull(result);
        Assert.Equal(org, result!.Name);
        Assert.Equal(org, result.GithubLogin);
        Assert.Empty(result.Repos);
    }

    [Fact]
    public async Task UpdateSettingsReflectsAndPersists()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        var updated = await PatchSettings(client, org,
            new { setMembersCanCreateStacks = false, setPreferredVCS = "gitlab" });
        Assert.False(updated!.MembersCanCreateStacks);
        Assert.Equal("gitlab", updated.PreferredVcs);
        Assert.Equal(org, updated.Id);

        // A follow-up no-op update returns the persisted values (proves they round-tripped to Postgres).
        var reread = await PatchSettings(client, org, new { });
        Assert.False(reread!.MembersCanCreateStacks);
        Assert.Equal("gitlab", reread.PreferredVcs);
    }

    [Fact]
    public async Task SetSoleAdminPromotesMember()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        Seed(org, s => s.AddMember(org, "bob", "member"));

        using var ok = await client.PostAsJsonAsync($"/api/orgs/{org}/members/bob/set-admin", new { });
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        Assert.Equal("admin", Read(org, s => s.ListMembers(org)).Single(m => m.UserLogin == "bob").Role);

        using var missing = await client.PostAsJsonAsync($"/api/orgs/{org}/members/ghost/set-admin", new { });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task ListUsersWithRoleReturnsMatchingMembers()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var roleId = Read(org, s =>
        {
            var role = s.CreateRole(org, new PermissionDescriptorBase { Name = "deployer" });
            s.AddMember(org, "alice", role.Id);
            s.AddMember(org, "carol", "admin");
            return role.Id;
        });

        var result = await client.GetFromJsonAsync<ListUsersWithRoleResponse>(
            $"/api/orgs/{org}/roles/{roleId}/users");

        Assert.NotNull(result);
        Assert.Single(result!.Users);
        Assert.Equal("alice", result.Users[0].GithubLogin);
    }

    [Fact]
    public async Task ListAvailableScopesReturnsNonEmptyGroups()
    {
        using var client = app.CreateClient();

        var result = await client.GetFromJsonAsync<Dictionary<string, List<RbacScopeGroup>>>(
            $"/api/orgs/{NewOrg()}/roles/scopes");

        Assert.NotNull(result);
        var groups = Assert.Single(result!).Value;
        Assert.NotEmpty(groups);
        Assert.Contains(groups, g => g.Name == "stack");
        Assert.All(groups, g => Assert.NotEmpty(g.Scopes));
    }

    [Fact]
    public async Task UpdateDefaultRoleSetsTheFlag()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var roleId = Read(org, s => s.CreateRole(org, new PermissionDescriptorBase { Name = "member+" }).Id);

        using var ok = await client.PatchAsJsonAsync($"/api/orgs/{org}/roles/{roleId}/default", new { });
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        Assert.True(Read(org, s => s.GetRole(org, roleId))!.IsOrgDefault);

        using var missing = await client.PatchAsJsonAsync($"/api/orgs/{org}/roles/ghost/default", new { });
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private async Task<OrganizationMetadata?> PatchSettings(HttpClient client, string org, object body)
    {
        using var resp = await client.PatchAsJsonAsync($"/api/orgs/{org}", body);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<OrganizationMetadata>();
    }

    private void Seed(string org, Action<IIdentityStore> seed)
        => Read(org, s => { seed(s); return 0; });

    private T Read<T>(string org, Func<IIdentityStore, T> read)
    {
        using var scope = app.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<IIdentityStore>());
    }
}
