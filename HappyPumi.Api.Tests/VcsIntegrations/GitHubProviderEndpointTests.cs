using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using HappyPumi.Api.Tests.Esc;
using HappyPumi.Api.Vcs;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPumi.Api.Tests.VcsIntegrations;

/// <summary>
/// Component tests for the PR2 GitHub provider endpoints (setup / access / org-teams / create-team / generic
/// repo+branch listing). Records are seeded through <see cref="IVcsIntegrationStore"/> from the app DI scope.
/// The default app is GitHub-unconfigured (no token) so list calls degrade to empty; one test overrides the
/// typed client's handler with a <see cref="StubHttpHandler"/> to exercise the configured → data path.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class GitHubProviderEndpointTests(HappyPumiApp app)
{
    private const string ReposJson =
        "[{\"id\":1,\"name\":\"widget\",\"full_name\":\"octo/widget\",\"owner\":{\"login\":\"octo\"}}]";

    private static string NewOrg() => "ghp-" + Guid.NewGuid().ToString("N");

    private StoredVcsIntegration Seed(StoredVcsIntegration integration)
    {
        using var scope = app.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IVcsIntegrationStore>().Create(integration);
    }

    private static StoredVcsIntegration GitHub(string org) => new()
    {
        Id = "seed", Org = org, Kind = "github", Name = "octo", AccountName = "octo",
    };

    private static StoredVcsIntegration AzureDevOps(string org) => new()
    {
        Id = "seed", Org = org, Kind = "azure-devops", AccountName = "contoso",
        BaseUrl = "https://dev.azure.com/contoso", AzureProject = "Widgets",
    };

    /// <summary>A client backed by a host where GitHub is configured and the GitHub HTTP calls hit the stub.</summary>
    private HttpClient ConfiguredClient(StubHttpHandler handler)
    {
        var factory = app.WithWebHostBuilder(b =>
        {
            b.UseSetting("Vcs:GitHub:Token", "ghs_test");
            b.ConfigureTestServices(s =>
                s.AddHttpClient<GitHubVcsProvider>().ConfigurePrimaryHttpMessageHandler(() => handler));
        });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", "dev");
        return client;
    }

    [Fact]
    public async Task StartGitHubSetupReturnsInstallUrlAndCreatesPendingRecord()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();

        var resp = await client.PostAsync($"/api/console/orgs/{org}/integrations/github",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<GitHubSetupResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.InstallationUrl));

        var list = await client.GetFromJsonAsync<ListGitHubIntegrationsResponse>(
            $"/api/console/orgs/{org}/integrations/github");
        Assert.Single(list!.Integrations);
    }

    [Fact]
    public async Task GetGitHubAccessReportsNotConfiguredButListsIntegrationOrgs()
    {
        var org = NewOrg();
        Seed(GitHub(org));
        using var client = app.CreateAuthedClient();

        var resp = await client.GetFromJsonAsync<VcsGitHubAccessResponse>(
            $"/api/console/orgs/{org}/integrations/github/access-status");

        Assert.False(resp!.HasUserToken); // default app has no Vcs:GitHub:Token
        Assert.True(resp.HasIntegration);
        Assert.Contains("octo", resp.AvailableOrgs);
    }

    [Fact]
    public async Task ListGitHubOrganizationTeamsEmptyWhenUnconfigured()
    {
        using var client = app.CreateAuthedClient();
        var resp = await client.GetFromJsonAsync<ListGitHubOrganizationTeamsResponse>("/api/user/github/octo/teams");
        Assert.Empty(resp!.Teams);
    }

    [Fact]
    public async Task CreateGitHubTeamCreatesGitHubBackedTeam()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();

        var resp = await client.PostAsJsonAsync($"/api/orgs/{org}/teams/github",
            new CreateGitHubTeamRequest { GithubTeamId = 7 });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var team = await resp.Content.ReadFromJsonAsync<Team>();
        Assert.Equal("github-7", team!.Name);
        Assert.Equal("github", team.Kind);

        using var scope = app.Services.CreateScope();
        var stored = scope.ServiceProvider.GetRequiredService<IIdentityStore>().GetTeam(org, "github-7");
        Assert.NotNull(stored);
        Assert.Equal("github", stored!.Kind);
    }

    [Fact]
    public async Task CreateGitHubTeamRejectsMissingTeamId()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();
        var resp = await client.PostAsJsonAsync($"/api/orgs/{org}/teams/github", new CreateGitHubTeamRequest());
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ListReposReturns404ForUnknownIntegration()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();
        var r = await client.GetAsync($"/api/console/orgs/{org}/integrations/github/{Guid.NewGuid():N}/repos");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task ListReposEmptyWhenGitHubUnconfigured()
    {
        var org = NewOrg();
        var gh = Seed(GitHub(org));
        using var client = app.CreateAuthedClient();

        var resp = await client.GetFromJsonAsync<ListVcsReposResponse>(
            $"/api/console/orgs/{org}/integrations/github/{gh.Id}/repos");
        Assert.Empty(resp!.Repos);
    }

    [Fact]
    public async Task ListBranchesEmptyWhenGitHubUnconfigured()
    {
        var org = NewOrg();
        var gh = Seed(GitHub(org));
        using var client = app.CreateAuthedClient();

        var resp = await client.GetFromJsonAsync<ListVcsBranchesResponse>(
            $"/api/console/orgs/{org}/integrations/github/{gh.Id}/repos/octo%2Fwidget/branches");
        Assert.Empty(resp!.Branches);
    }

    [Fact]
    public async Task ListReposEmptyForAzureDevOpsWithoutCrashing()
    {
        var org = NewOrg();
        var ado = Seed(AzureDevOps(org));
        using var client = app.CreateAuthedClient();

        var resp = await client.GetFromJsonAsync<ListVcsReposResponse>(
            $"/api/console/orgs/{org}/integrations/azure-devops/{ado.Id}/repos");
        Assert.Empty(resp!.Repos);
    }

    [Fact]
    public async Task ListReposReturnsDataWhenConfiguredViaStub()
    {
        var org = NewOrg();
        var gh = Seed(GitHub(org));
        var handler = new StubHttpHandler(ReposJson);
        using var client = ConfiguredClient(handler);

        var resp = await client.GetFromJsonAsync<ListVcsReposResponse>(
            $"/api/console/orgs/{org}/integrations/github/{gh.Id}/repos");

        var repo = Assert.Single(resp!.Repos);
        Assert.Equal("octo/widget", repo.Id);
        Assert.Equal("https://api.github.com/orgs/octo/repos?per_page=100", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListRepoDestinationsReturnsDataWhenConfiguredViaStub()
    {
        var org = NewOrg();
        var gh = Seed(GitHub(org));
        var handler = new StubHttpHandler(ReposJson);
        using var client = ConfiguredClient(handler);

        var resp = await client.GetFromJsonAsync<ListVcsReposResponse>(
            $"/api/console/orgs/{org}/integrations/github/{gh.Id}/repos/destinations");
        Assert.Single(resp!.Repos);
    }
}
