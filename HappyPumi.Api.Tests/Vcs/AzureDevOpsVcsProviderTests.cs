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
/// Unit tests for <see cref="AzureDevOpsVcsProvider"/> driven by the ESC <see cref="StubHttpHandler"/>: assert
/// the OAuth authorize-URL build, the token-exchange POST shape, and the orgs/projects/repos/branches REST
/// calls (method/URL/Bearer auth) plus parsing of canned responses. No live network.
/// </summary>
public sealed class AzureDevOpsVcsProviderTests
{
    private const string ReposJson =
        "{\"value\":[{\"id\":\"r1\",\"name\":\"widget\",\"project\":{\"name\":\"Widgets\"}}]}";
    private const string RefsJson =
        "{\"value\":[{\"name\":\"refs/heads/main\",\"isLocked\":true},{\"name\":\"refs/heads/dev\",\"isLocked\":false}]}";
    private const string ProfileJson = "{\"id\":\"member-1\"}";
    private const string AccountsJson =
        "{\"value\":[{\"accountId\":\"a1\",\"accountName\":\"contoso\"}]}";
    private const string ProjectsJson =
        "{\"value\":[{\"id\":\"p1\",\"name\":\"Widgets\"}]}";
    private const string TokenJson = "{\"access_token\":\"ado-token\",\"token_type\":\"bearer\"}";

    private static AzureDevOpsVcsProvider Provider(StubHttpHandler handler, bool configured = true)
    {
        var settings = new Dictionary<string, string?>();
        if (configured)
        {
            settings["Vcs:AzureDevOps:ClientId"] = "client-123";
            settings["Vcs:AzureDevOps:ClientSecret"] = "secret-abc";
            settings["Vcs:AzureDevOps:RedirectUri"] = "https://happypumi.test/cb";
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new AzureDevOpsVcsProvider(new HttpClient(handler), config);
    }

    private static StoredVcsIntegration Ado(string? credential = "ado-token") => new()
    {
        Id = "i1", Org = "acme", Kind = "azure-devops",
        AccountName = "contoso", AzureProject = "Widgets", Credential = credential,
    };

    [Fact]
    public void BuildAuthorizationUrlIncludesClientStateScopeAndRedirect()
    {
        var url = Provider(new StubHttpHandler(TokenJson)).BuildAuthorizationUrl("st8");

        Assert.StartsWith("https://app.vssps.visualstudio.com/oauth2/authorize?", url);
        Assert.Contains("client_id=client-123", url);
        Assert.Contains("response_type=Assertion", url);
        Assert.Contains("state=st8", url);
        Assert.Contains("scope=vso.code%20vso.project", url);
        Assert.Contains("redirect_uri=https%3A%2F%2Fhappypumi.test%2Fcb", url);
    }

    [Fact]
    public void BuildAuthorizationUrlIsEmptyWhenUnconfigured()
        => Assert.Equal("", Provider(new StubHttpHandler(TokenJson), configured: false).BuildAuthorizationUrl("st8"));

    [Fact]
    public async Task ExchangeCodePostsJwtBearerGrantToTokenEndpoint()
    {
        var handler = new StubHttpHandler(TokenJson);
        var token = await Provider(handler).ExchangeCodeAsync("the-code", CancellationToken.None);

        Assert.Equal("ado-token", token);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://app.vssps.visualstudio.com/oauth2/token", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Ajwt-bearer", handler.LastBody);
        Assert.Contains("assertion=the-code", handler.LastBody);
        Assert.Contains("client_assertion=secret-abc", handler.LastBody);
    }

    [Fact]
    public async Task ExchangeCodeReturnsNullWhenUnconfigured()
    {
        var handler = new StubHttpHandler(TokenJson);
        Assert.Null(await Provider(handler, configured: false).ExchangeCodeAsync("c", CancellationToken.None));
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ListReposHitsRepositoriesEndpointWithBearerAndParses()
    {
        var handler = new StubHttpHandler(ReposJson);
        var repos = await Provider(handler).ListReposAsync(Ado(), CancellationToken.None);

        var repo = Assert.Single(repos);
        Assert.Equal("r1", repo.Id);
        Assert.Equal("widget", repo.Name);
        Assert.Equal("Widgets", repo.Owner);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("https://dev.azure.com/contoso/Widgets/_apis/git/repositories?api-version=7.1",
            handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("ado-token", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ListBranchesHitsRefsEndpointAndStripsHeadsPrefix()
    {
        var handler = new StubHttpHandler(RefsJson);
        var branches = await Provider(handler).ListBranchesAsync(Ado(), "r1", CancellationToken.None);

        Assert.Equal(2, branches.Count);
        Assert.Contains(branches, b => b.Name == "main" && b.IsProtected);
        Assert.Contains(branches, b => b.Name == "dev" && !b.IsProtected);
        Assert.Equal("https://dev.azure.com/contoso/Widgets/_apis/git/repositories/r1/refs?api-version=7.1",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListOrganizationsResolvesMemberThenAccounts()
    {
        var handler = new StubHttpHandler(ProfileJson).ThenRespondWith(AccountsJson);
        var orgs = await Provider(handler).ListOrganizationsAsync("ado-token", CancellationToken.None);

        var org = Assert.Single(orgs);
        Assert.Equal("contoso", org.Name);
        Assert.Equal("a1", org.Id);
        Assert.Equal("https://dev.azure.com/contoso", org.AccountUrl);
        Assert.Equal("https://app.vssps.visualstudio.com/_apis/accounts?memberId=member-1&api-version=7.1",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListProjectsHitsProjectsEndpointAndParses()
    {
        var handler = new StubHttpHandler(ProjectsJson);
        var projects = await Provider(handler).ListProjectsAsync("contoso", "ado-token", CancellationToken.None);

        var project = Assert.Single(projects);
        Assert.Equal("p1", project.Id);
        Assert.Equal("Widgets", project.Name);
        Assert.Equal("https://dev.azure.com/contoso/_apis/projects?api-version=7.1",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task NoTokenMakesNoCallAndReturnsEmpty()
    {
        var handler = new StubHttpHandler(ReposJson);
        var provider = Provider(handler);

        Assert.Empty(await provider.ListReposAsync(Ado(credential: null), CancellationToken.None));
        Assert.Empty(await provider.ListBranchesAsync(Ado(credential: null), "r1", CancellationToken.None));
        Assert.Empty(await provider.ListOrganizationsAsync(null, CancellationToken.None));
        Assert.Empty(await provider.ListProjectsAsync("contoso", null, CancellationToken.None));
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task AzureDevOpsErrorResponseDegradesToEmpty()
    {
        var handler = new StubHttpHandler("{\"message\":\"Unauthorized\"}", System.Net.HttpStatusCode.Unauthorized);
        var repos = await Provider(handler).ListReposAsync(Ado(), CancellationToken.None);
        Assert.Empty(repos);
    }
}
