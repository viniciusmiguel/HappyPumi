using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the access-token endpoints (personal / org / team + tokens-by-role). Proves the
/// issue-once contract (the plaintext is returned once at creation and the list never echoes a value), that
/// revoke is idempotent (404 on a missing/already-revoked id), and that the three scopes are isolated.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class AccessTokenTests(HappyPumiApp app)
{
    private const string Org = "happypumi";

    private static async Task<CreateAccessTokenResponse> Issue(HttpClient client, string url, object body)
    {
        var res = await client.PostAsJsonAsync(url, body);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CreateAccessTokenResponse>())!;
    }

    private static async Task<ListAccessTokensResponse> List(HttpClient client, string url)
        => (await client.GetFromJsonAsync<ListAccessTokensResponse>(url))!;

    [Fact]
    public async Task PersonalTokenIssueListRevokeLifecycle()
    {
        using var client = app.CreateAuthedClient();
        var desc = $"personal-{Guid.NewGuid():N}";

        var created = await Issue(client, "/api/user/tokens", new CreatePersonalAccessTokenRequest { Description = desc });
        Assert.StartsWith("pul-", created.TokenValue); // shown once at creation
        Assert.NotEmpty(created.Id);

        var listed = await List(client, "/api/user/tokens");
        var mine = listed.Tokens.Single(t => t.Id == created.Id);
        Assert.Equal(desc, mine.Description);

        using var del = await client.DeleteAsync($"/api/user/tokens/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        using var again = await client.DeleteAsync($"/api/user/tokens/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode); // already revoked
    }

    [Fact]
    public async Task ListNeverExposesTheTokenValueOrHash()
    {
        using var client = app.CreateAuthedClient();
        var created = await Issue(client, "/api/user/tokens",
            new CreatePersonalAccessTokenRequest { Description = $"secret-{Guid.NewGuid():N}" });

        using var raw = await client.GetAsync("/api/user/tokens");
        var json = await raw.Content.ReadAsStringAsync();
        Assert.DoesNotContain(created.TokenValue, json); // plaintext never echoed
        Assert.DoesNotContain("pul-", json);
    }

    [Fact]
    public async Task OrgTokenIssueListRevokeLifecycle()
    {
        using var client = app.CreateAuthedClient();
        var name = $"org-{Guid.NewGuid():N}";

        var created = await Issue(client, $"/api/orgs/{Org}/tokens",
            new CreateOrgAccessTokenRequest { Name = name, Description = "ci" });
        Assert.StartsWith("pul-", created.TokenValue);

        var listed = await List(client, $"/api/orgs/{Org}/tokens");
        Assert.Contains(listed.Tokens, t => t.Id == created.Id && t.Name == name);

        using var del = await client.DeleteAsync($"/api/orgs/{Org}/tokens/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task OrgTokensFilterByRole()
    {
        using var client = app.CreateAuthedClient();
        var roleId = $"role-{Guid.NewGuid():N}";
        var withRole = await Issue(client, $"/api/orgs/{Org}/tokens",
            new CreateOrgAccessTokenRequest { Name = $"r-{Guid.NewGuid():N}", RoleId = roleId });
        await Issue(client, $"/api/orgs/{Org}/tokens",
            new CreateOrgAccessTokenRequest { Name = $"n-{Guid.NewGuid():N}" });

        var byRole = await List(client, $"/api/orgs/{Org}/roles/{roleId}/tokens");
        Assert.Single(byRole.Tokens);
        Assert.Equal(withRole.Id, byRole.Tokens[0].Id);
        Assert.Equal(roleId, byRole.Tokens[0].Role!.Id);
    }

    [Fact]
    public async Task TeamTokenIssueListRevokeLifecycle()
    {
        using var client = app.CreateAuthedClient();
        const string team = "platform";
        var name = $"team-{Guid.NewGuid():N}";

        var created = await Issue(client, $"/api/orgs/{Org}/teams/{team}/tokens",
            new CreateTeamAccessTokenRequest { Name = name, Description = "deploy" });

        var listed = await List(client, $"/api/orgs/{Org}/teams/{team}/tokens");
        Assert.Contains(listed.Tokens, t => t.Id == created.Id);

        using var del = await client.DeleteAsync($"/api/orgs/{Org}/teams/{team}/tokens/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task ScopesAreIsolated()
    {
        using var client = app.CreateAuthedClient();
        const string team = "isolation";
        var personal = await Issue(client, "/api/user/tokens",
            new CreatePersonalAccessTokenRequest { Description = $"p-{Guid.NewGuid():N}" });
        var orgTok = await Issue(client, $"/api/orgs/{Org}/tokens",
            new CreateOrgAccessTokenRequest { Name = $"o-{Guid.NewGuid():N}" });
        var teamTok = await Issue(client, $"/api/orgs/{Org}/teams/{team}/tokens",
            new CreateTeamAccessTokenRequest { Name = $"t-{Guid.NewGuid():N}" });

        var orgList = await List(client, $"/api/orgs/{Org}/tokens");
        Assert.DoesNotContain(orgList.Tokens, t => t.Id == personal.Id || t.Id == teamTok.Id);

        var teamList = await List(client, $"/api/orgs/{Org}/teams/{team}/tokens");
        Assert.DoesNotContain(teamList.Tokens, t => t.Id == personal.Id || t.Id == orgTok.Id);
    }

    [Fact]
    public async Task TokenEndpointsRequireAuthentication()
    {
        using var anon = app.CreateClient();
        using var personal = await anon.GetAsync("/api/user/tokens");
        Assert.Equal(HttpStatusCode.Unauthorized, personal.StatusCode);
        using var org = await anon.GetAsync($"/api/orgs/{Org}/tokens");
        Assert.Equal(HttpStatusCode.Unauthorized, org.StatusCode);
    }
}
