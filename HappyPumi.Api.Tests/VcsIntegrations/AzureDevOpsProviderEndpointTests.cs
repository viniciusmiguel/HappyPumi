using System;
using System.Collections.Generic;
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
/// Component tests for the PR3 Azure DevOps provider endpoints (create / OAuth initiate+complete /
/// access-status / setup orgs+projects / generic repo dispatch). Records are seeded through
/// <see cref="IVcsIntegrationStore"/> from the app DI scope. The default app is ADO-unconfigured so list
/// calls degrade to empty; <see cref="ConfiguredClient"/> overrides the typed client's handler with a
/// <see cref="StubHttpHandler"/> and sets the OAuth client to exercise the configured → data path.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class AzureDevOpsProviderEndpointTests(HappyPumiApp app)
{
    private const string ReposJson =
        "{\"value\":[{\"id\":\"r1\",\"name\":\"widget\",\"project\":{\"name\":\"Widgets\"}}]}";
    private const string ProfileJson = "{\"id\":\"member-1\"}";
    private const string AccountsJson = "{\"value\":[{\"accountId\":\"a1\",\"accountName\":\"contoso\"}]}";
    private const string ProjectsJson = "{\"value\":[{\"id\":\"p1\",\"name\":\"Widgets\"}]}";
    private const string TokenJson = "{\"access_token\":\"ado-token\",\"token_type\":\"bearer\"}";

    private static string NewOrg() => "ado-" + Guid.NewGuid().ToString("N");

    private StoredVcsIntegration Seed(StoredVcsIntegration integration)
    {
        using var scope = app.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IVcsIntegrationStore>().Create(integration);
    }

    private StoredVcsIntegration? Reload(string org, string id)
    {
        using var scope = app.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IVcsIntegrationStore>().Get(org, id);
    }

    private static StoredVcsIntegration Ado(string org, string? credential = null) => new()
    {
        Id = "seed", Org = org, Kind = "azure-devops", Name = "contoso", AccountName = "contoso",
        AzureProject = "Widgets", BaseUrl = "https://dev.azure.com/contoso", Credential = credential,
    };

    /// <summary>A client backed by a host where the ADO OAuth client is configured and calls hit the stub.</summary>
    private HttpClient ConfiguredClient(StubHttpHandler handler)
    {
        var factory = app.WithWebHostBuilder(b =>
        {
            b.UseSetting("Vcs:AzureDevOps:ClientId", "client-123");
            b.UseSetting("Vcs:AzureDevOps:ClientSecret", "secret-abc");
            b.UseSetting("Vcs:AzureDevOps:RedirectUri", "https://happypumi.test/cb");
            b.ConfigureTestServices(s =>
                s.AddHttpClient<AzureDevOpsVcsProvider>().ConfigurePrimaryHttpMessageHandler(() => handler));
        });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", "dev");
        return client;
    }

    [Fact]
    public async Task CreateAzureDevOpsSetupCreatesRecord()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();

        var resp = await client.PostAsJsonAsync($"/api/console/orgs/{org}/integrations/azure-devops",
            new UpdateAzureDevOpsAppIntegrationRequest { OrganizationName = "contoso", ProjectId = "Widgets" });
        Assert.True(resp.IsSuccessStatusCode, $"create returned {resp.StatusCode}");

        var list = await client.GetFromJsonAsync<ListAzureDevOpsIntegrationsResponse>(
            $"/api/console/orgs/{org}/integrations/azure-devops");
        Assert.Single(list!.Integrations);
    }

    [Fact]
    public async Task InitiateOAuthReturnsAuthorizationUrlAndSession()
    {
        var org = NewOrg();
        var handler = new StubHttpHandler(TokenJson);
        using var client = ConfiguredClient(handler);

        var resp = await client.PostAsJsonAsync(
            $"/api/console/orgs/{org}/integrations/azure-devops/oauth/initiate",
            new InitiateOAuthRequest { Provider = new CloudSetupProvider { Name = "azure-devops" } });

        var body = await resp.Content.ReadFromJsonAsync<InitiateOAuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.SessionId));
        Assert.Contains("app.vssps.visualstudio.com/oauth2/authorize", body.Url);
        Assert.Contains($"state={body.SessionId}", body.Url);
    }

    [Fact]
    public async Task CompleteOAuthExchangesCodeAndPersistsTokenOnIntegration()
    {
        var org = NewOrg();
        var ado = Seed(Ado(org));
        var handler = new StubHttpHandler(TokenJson);
        using var client = ConfiguredClient(handler);

        var resp = await client.PostAsJsonAsync(
            $"/api/console/orgs/{org}/integrations/azure-devops/oauth/complete",
            new CompleteOAuthRequest { Code = "the-code", SessionId = "s1", Provider = new CloudSetupProvider { Name = "azure-devops" } });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("ado-token", Reload(org, ado.Id)!.Credential);
    }

    [Fact]
    public async Task AccessStatusNotConfiguredWhenNoToken()
    {
        var org = NewOrg();
        Seed(Ado(org));
        using var client = app.CreateAuthedClient();

        var resp = await client.GetFromJsonAsync<AzureDevOpsAccessResponse>(
            $"/api/console/orgs/{org}/integrations/azure-devops/access-status");

        Assert.True(resp!.HasIntegration);
        Assert.False(resp.HasUserToken);
        Assert.Empty(resp.AvailableOrgs ?? new List<AzureDevOpsOrganization>());
    }

    [Fact]
    public async Task AccessStatusReportsTokenAndOrgsWhenConfigured()
    {
        var org = NewOrg();
        Seed(Ado(org, credential: "ado-token"));
        var handler = new StubHttpHandler(ProfileJson).ThenRespondWith(AccountsJson);
        using var client = ConfiguredClient(handler);

        var resp = await client.GetFromJsonAsync<AzureDevOpsAccessResponse>(
            $"/api/console/orgs/{org}/integrations/azure-devops/access-status");

        Assert.True(resp!.HasUserToken);
        Assert.Contains(resp.AvailableOrgs!, o => o.Name == "contoso");
    }

    [Fact]
    public async Task ListOrganizationsEmptyWhenUnconfigured()
    {
        var org = NewOrg();
        Seed(Ado(org));
        using var client = app.CreateAuthedClient();

        var orgs = await client.GetFromJsonAsync<List<AzureDevOpsOrganization>>(
            $"/api/console/orgs/{org}/integrations/azure-devops/setup/organizations");
        Assert.Empty(orgs!);
    }

    [Fact]
    public async Task ListOrganizationsReturnsDataWhenConfigured()
    {
        var org = NewOrg();
        Seed(Ado(org, credential: "ado-token"));
        var handler = new StubHttpHandler(ProfileJson).ThenRespondWith(AccountsJson);
        using var client = ConfiguredClient(handler);

        var orgs = await client.GetFromJsonAsync<List<AzureDevOpsOrganization>>(
            $"/api/console/orgs/{org}/integrations/azure-devops/setup/organizations");
        Assert.Contains(orgs!, o => o.Name == "contoso");
    }

    [Fact]
    public async Task ListProjectsReturnsDataWhenConfigured()
    {
        var org = NewOrg();
        Seed(Ado(org, credential: "ado-token"));
        var handler = new StubHttpHandler(ProjectsJson);
        using var client = ConfiguredClient(handler);

        var projects = await client.GetFromJsonAsync<List<AzureDevOpsProject>>(
            $"/api/console/orgs/{org}/integrations/azure-devops/setup/organizations/contoso/projects");
        Assert.Contains(projects!, p => p.Name == "Widgets");
    }

    [Fact]
    public async Task ListReposDispatchesToAzureDevOpsWhenConfigured()
    {
        var org = NewOrg();
        var ado = Seed(Ado(org, credential: "ado-token"));
        var handler = new StubHttpHandler(ReposJson);
        using var client = ConfiguredClient(handler);

        var resp = await client.GetFromJsonAsync<ListVcsReposResponse>(
            $"/api/console/orgs/{org}/integrations/azure-devops/{ado.Id}/repos");

        var repo = Assert.Single(resp!.Repos);
        Assert.Equal("r1", repo.Id);
        Assert.Equal("https://dev.azure.com/contoso/Widgets/_apis/git/repositories?api-version=7.1",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task ListReposEmptyWhenAzureDevOpsUnconfigured()
    {
        var org = NewOrg();
        var ado = Seed(Ado(org));
        using var client = app.CreateAuthedClient();

        var resp = await client.GetFromJsonAsync<ListVcsReposResponse>(
            $"/api/console/orgs/{org}/integrations/azure-devops/{ado.Id}/repos");
        Assert.Empty(resp!.Repos);
    }
}
