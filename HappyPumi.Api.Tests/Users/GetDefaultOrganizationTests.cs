using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Users;

/// <summary>
/// Component tests for GET /api/user/organizations/default (Tier 0). The CLI calls this to resolve the
/// org for unqualified stack names, so the response must carry a non-empty org handle.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class GetDefaultOrganizationTests(HappyPumiApp app)
{
    [Fact]
    public async Task Returns200()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/api/user/organizations/default");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // The default org must match the login GetCurrentUser advertises, or unqualified stack names
    // resolve to the wrong org.
    [Fact]
    public async Task DefaultOrgMatchesTheCurrentUserLogin()
    {
        using var client = app.CreateAuthedClient(); // also calls /api/user, which now requires a token

        var defaultOrg = await client.GetFromJsonAsync<AppGetDefaultOrganizationResponse>(
            "/api/user/organizations/default");
        var user = await client.GetFromJsonAsync<User>("/api/user");

        Assert.NotNull(defaultOrg);
        Assert.Equal(user!.GithubLogin, defaultOrg!.GitHubLogin);
    }
}
