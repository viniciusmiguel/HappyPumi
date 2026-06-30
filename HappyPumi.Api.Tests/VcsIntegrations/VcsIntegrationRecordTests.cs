using System;
using System.Net;
using System.Net.Http.Json;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPumi.Api.Tests.VcsIntegrations;

/// <summary>
/// Component tests for the provider-neutral VCS integration-record endpoints (list / get / update / delete)
/// for github / github-enterprise / azure-devops. Records are seeded through <see cref="IVcsIntegrationStore"/>
/// resolved from the app's DI scope (no create endpoint ships in PR1).
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class VcsIntegrationRecordTests(HappyPumiApp app)
{
    private static string NewOrg() => "vcs-" + Guid.NewGuid().ToString("N");

    private StoredVcsIntegration Seed(StoredVcsIntegration integration)
    {
        using var scope = app.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IVcsIntegrationStore>();
        return store.Create(integration);
    }

    private static StoredVcsIntegration GitHub(string org, string kind = "github") => new()
    {
        Id = "seed", Org = org, Kind = kind, Name = "octocat", AccountName = "octocat", AccountId = 42,
        AvatarUrl = "https://avatars.example/octo.png",
        BaseUrl = kind == "github-enterprise" ? "https://ghe.example.com" : null,
    };

    private static StoredVcsIntegration AzureDevOps(string org) => new()
    {
        Id = "seed", Org = org, Kind = "azure-devops", AccountName = "contoso",
        BaseUrl = "https://dev.azure.com/contoso", AzureProject = "Widgets",
    };

    [Fact]
    public async Task ListAllEmptyForUnknownOrg()
    {
        using var client = app.CreateAuthedClient();
        var resp = await client.GetFromJsonAsync<ListVcsIntegrationSummariesResponse>(
            $"/api/console/orgs/{NewOrg()}/integrations");
        Assert.NotNull(resp);
        Assert.Empty(resp!.Integrations);
    }

    [Fact]
    public async Task ListAllReturnsSummariesAcrossProviders()
    {
        var org = NewOrg();
        Seed(GitHub(org));
        Seed(AzureDevOps(org));
        using var client = app.CreateAuthedClient();

        var resp = await client.GetFromJsonAsync<ListVcsIntegrationSummariesResponse>(
            $"/api/console/orgs/{org}/integrations");

        Assert.NotNull(resp);
        Assert.Equal(2, resp!.Integrations.Count);
        Assert.Contains(resp.Integrations, i => i.VcsProvider == "github" && i.HasIndividualAccess);
        var ado = Assert.Single(resp.Integrations, i => i.VcsProvider == "azure-devops");
        Assert.Equal("dev.azure.com", ado.Host);
    }

    [Fact]
    public async Task ListGitHubFiltersByKindAndMaps()
    {
        var org = NewOrg();
        var gh = Seed(GitHub(org));
        Seed(AzureDevOps(org));
        using var client = app.CreateAuthedClient();

        var resp = await client.GetFromJsonAsync<ListGitHubIntegrationsResponse>(
            $"/api/console/orgs/{org}/integrations/github");

        Assert.NotNull(resp);
        var details = Assert.Single(resp!.Integrations);
        Assert.Equal(gh.Id, details.Id);
        Assert.Equal("octocat", details.AccountName);
        Assert.Equal(42, details.AccountId);
        Assert.False(details.IsSelfHosted);
        Assert.True(details.HasContentsPermission);
    }

    [Fact]
    public async Task ListGitHubEnterpriseReturnsOnlySelfHosted()
    {
        var org = NewOrg();
        Seed(GitHub(org));
        Seed(GitHub(org, "github-enterprise"));
        using var client = app.CreateAuthedClient();

        var resp = await client.GetFromJsonAsync<ListGitHubIntegrationsResponse>(
            $"/api/console/orgs/{org}/integrations/github-enterprise");

        Assert.NotNull(resp);
        var details = Assert.Single(resp!.Integrations);
        Assert.True(details.IsSelfHosted);
    }

    [Fact]
    public async Task ListAzureDevOpsMapsOrgAndProject()
    {
        var org = NewOrg();
        Seed(AzureDevOps(org));
        using var client = app.CreateAuthedClient();

        var resp = await client.GetFromJsonAsync<ListAzureDevOpsIntegrationsResponse>(
            $"/api/console/orgs/{org}/integrations/azure-devops");

        Assert.NotNull(resp);
        var details = Assert.Single(resp!.Integrations);
        Assert.Equal("contoso", details.Organization!.Name);
        Assert.Equal("Widgets", details.Project!.Name);
        Assert.True(details.Valid);
    }
}
