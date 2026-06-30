using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.State;
using HappyPumi.Api.Tests.Esc;
using HappyPumi.Api.Vcs;
using Microsoft.Extensions.Configuration;

namespace HappyPumi.Api.Tests.Vcs;

/// <summary>
/// Unit tests for <see cref="GitHubVcsProvider"/> driven by the ESC <see cref="StubHttpHandler"/>: assert the
/// outgoing GitHub REST shape (method/URL/auth header) and the parsing of canned responses. No live network.
/// </summary>
public sealed class GitHubVcsProviderTests
{
    private const string ReposJson =
        "[{\"id\":1,\"name\":\"widget\",\"full_name\":\"octo/widget\",\"owner\":{\"login\":\"octo\"}}]";
    private const string BranchesJson =
        "[{\"name\":\"main\",\"protected\":true},{\"name\":\"dev\",\"protected\":false}]";
    private const string TeamsJson =
        "[{\"id\":7,\"name\":\"Platform\",\"slug\":\"platform\",\"description\":\"infra\"}]";

    private static GitHubVcsProvider Provider(StubHttpHandler handler, string? token = "ghs_test", string? apiBase = null)
    {
        var settings = new Dictionary<string, string?> { ["Vcs:GitHub:Token"] = token };
        if (apiBase is not null)
            settings["Vcs:GitHub:ApiBaseUrl"] = apiBase;
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new GitHubVcsProvider(new HttpClient(handler), config);
    }

    private static StoredVcsIntegration GitHub(string? account = "octo", string? baseUrl = null) => new()
    {
        Id = "i1", Org = "acme", Kind = baseUrl is null ? "github" : "github-enterprise",
        AccountName = account, BaseUrl = baseUrl,
    };

    [Fact]
    public async Task ListReposHitsOrgEndpointWithBearerAuthAndParses()
    {
        var handler = new StubHttpHandler(ReposJson);
        var repos = await Provider(handler).ListReposAsync(GitHub(), CancellationToken.None);

        var repo = Assert.Single(repos);
        Assert.Equal("octo/widget", repo.Id);
        Assert.Equal("widget", repo.Name);
        Assert.Equal("octo", repo.Owner);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("https://api.github.com/orgs/octo/repos?per_page=100", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("ghs_test", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ListReposWithoutAccountUsesAuthenticatedUserEndpoint()
    {
        var handler = new StubHttpHandler(ReposJson);
        await Provider(handler).ListReposAsync(GitHub(account: null), CancellationToken.None);

        Assert.Equal("https://api.github.com/user/repos?per_page=100", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListBranchesHitsRepoBranchesEndpointAndParsesProtection()
    {
        var handler = new StubHttpHandler(BranchesJson);
        var branches = await Provider(handler).ListBranchesAsync(GitHub(), "octo/widget", CancellationToken.None);

        Assert.Equal(2, branches.Count);
        Assert.Contains(branches, b => b.Name == "main" && b.IsProtected);
        Assert.Contains(branches, b => b.Name == "dev" && !b.IsProtected);
        Assert.Equal("https://api.github.com/repos/octo/widget/branches?per_page=100",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListOrganizationTeamsHitsTeamsEndpointAndParses()
    {
        var handler = new StubHttpHandler(TeamsJson);
        var teams = await Provider(handler).ListOrganizationTeamsAsync("octo", integration: null, CancellationToken.None);

        var team = Assert.Single(teams);
        Assert.Equal(7, team.Id);
        Assert.Equal("Platform", team.Name);
        Assert.Equal("platform", team.Slug);
        Assert.False(team.KnownToPulumi);
        Assert.Equal("https://api.github.com/orgs/octo/teams?per_page=100", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GitHubEnterpriseUsesIntegrationBaseUrlUnderApiV3()
    {
        var handler = new StubHttpHandler(ReposJson);
        var ghe = GitHub(account: "octo", baseUrl: "https://ghe.example.com");
        await Provider(handler).ListReposAsync(ghe, CancellationToken.None);

        Assert.Equal("https://ghe.example.com/api/v3/orgs/octo/repos?per_page=100",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task UnconfiguredProviderMakesNoCallAndReturnsEmpty()
    {
        var handler = new StubHttpHandler(ReposJson);
        var provider = Provider(handler, token: null);

        Assert.False(await provider.IsConfiguredAsync());
        Assert.Empty(await provider.ListReposAsync(GitHub(), CancellationToken.None));
        Assert.Empty(await provider.ListBranchesAsync(GitHub(), "octo/widget", CancellationToken.None));
        Assert.Empty(await provider.ListOrganizationTeamsAsync("octo", null, CancellationToken.None));
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GitHubErrorResponseDegradesToEmpty()
    {
        var handler = new StubHttpHandler("{\"message\":\"Not Found\"}", System.Net.HttpStatusCode.NotFound);
        var repos = await Provider(handler).ListReposAsync(GitHub(), CancellationToken.None);
        Assert.Empty(repos);
    }
}
