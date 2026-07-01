using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Users;

/// <summary>
/// Component tests for the /api/user/* account surface (org-admin PR6) against the real Postgres-backed
/// <c>IUserAccountStore</c>. The endpoints are AllowAnonymous (matching GetCurrentUser's siblings), so the
/// plain client resolves the dev default login. Every endpoint must return 200/204 and never 500; the VCS
/// identity endpoints echo a <see cref="User"/> whose githubLogin stays non-empty (the CLI linchpin).
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class UserAccountTests(HappyPumiApp app)
{
    private static StringContent EmptyJson() => new("{}", System.Text.Encoding.UTF8, "application/json");

    [Fact]
    public async Task VerifiedEmailDefaultsToTrue()
    {
        using var client = app.CreateClient();

        var verified = await client.GetFromJsonAsync<bool>("/api/user/verified-email");

        Assert.True(verified);
    }

    [Fact]
    public async Task PendingEmailIsEmptyByDefault()
    {
        using var client = app.CreateClient();

        var pending = await client.GetFromJsonAsync<GetPendingEmailVerificationResponse>("/api/user/pending-emails");

        Assert.NotNull(pending);
        Assert.Equal(string.Empty, pending!.Email);
    }

    [Fact]
    public async Task DeletePendingEmailReturnsNoContent()
    {
        using var client = app.CreateClient();

        using var response = await client.DeleteAsync("/api/user/pending-emails");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PendingInvitesIsEmpty()
    {
        using var client = app.CreateClient();

        var invites = await client.GetFromJsonAsync<ListOrganizationInvitesResponse>("/api/user/pending-invites");

        Assert.NotNull(invites);
        Assert.Empty(invites!.Invites);
    }

    [Fact]
    public async Task UpdateDefaultOrganizationReturnsNoContent()
    {
        using var client = app.CreateClient();

        // The request binds orgName from the route; FastEndpoints' Endpoint<TReq> still negotiates a JSON
        // body, so send an empty object (the console/CLI post application/json with no meaningful payload).
        using var response = await client.PostAsync("/api/user/organizations/acme/default", EmptyJson());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ListIdentityProviderOrganizationsIsEmpty()
    {
        using var client = app.CreateClient();

        var orgs = await client.GetFromJsonAsync<ListGitHubOrganizationsResponse>("/api/user/vcs/organizations");

        Assert.NotNull(orgs);
        Assert.Empty(orgs!.Organizations);
    }

    [Fact]
    public async Task GitLabAppOrganizationsIsEmpty()
    {
        using var client = app.CreateClient();

        var groups = await client.GetFromJsonAsync<List<GitLabAppOrganization>>("/api/user/gitlab-app/organizations");

        Assert.NotNull(groups);
        Assert.Empty(groups!);
    }

    [Fact]
    public async Task DeleteIdentityProviderReturnsUserWithGithubLogin()
    {
        using var client = app.CreateClient();

        var user = await client.DeleteFromJsonAsync<User>("/api/user/vcs?identity=github");

        Assert.NotNull(user);
        Assert.False(string.IsNullOrWhiteSpace(user!.GithubLogin));
    }

    [Fact]
    public async Task SyncWithIdentityProviderReturnsUserWithGithubLogin()
    {
        using var client = app.CreateClient();

        using var response = await client.PostAsync("/api/user/vcs/sync?identity=github", EmptyJson());
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<User>();

        Assert.NotNull(user);
        Assert.False(string.IsNullOrWhiteSpace(user!.GithubLogin));
    }
}
