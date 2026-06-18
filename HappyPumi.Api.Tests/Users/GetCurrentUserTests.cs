using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Users;

/// <summary>
/// Component tests for GET /api/user (Tier 0 — login). This is the endpoint the Pulumi CLI
/// hits to validate a login, so its contract is load-bearing for the CLI integration tests.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class GetCurrentUserTests(HappyPumiApp app)
{
    [Fact]
    public async Task Returns200()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/api/user");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Regression guard for the login linchpin: the CLI's GetPulumiAccountDetails rejects the
    // login with "unexpected response from server" when githubLogin is empty. If this assertion
    // ever fails, `pulumi login` against HappyPumi breaks — see GetCurrentUserEndpoint.
    [Fact]
    public async Task GithubLoginIsNonEmpty()
    {
        using var client = app.CreateClient();

        var user = await client.GetFromJsonAsync<User>("/api/user");

        Assert.NotNull(user);
        Assert.False(string.IsNullOrWhiteSpace(user!.GithubLogin), "githubLogin must be non-empty for the CLI to accept the login");
    }
}
