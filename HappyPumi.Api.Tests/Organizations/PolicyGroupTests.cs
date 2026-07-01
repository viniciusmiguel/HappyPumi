using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the policy-group endpoints + org registry policy pack (policy-results PR2) against real
/// Postgres. Groups/packs are seeded through the real stores (resolved from a request scope); the endpoints
/// then read/mutate them. Unique org per test for independence.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class PolicyGroupTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task BatchUpdateAddsPackReflectedByGetGroup()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        SeedGroup(org, "prod");

        using var resp = await client.PatchAsJsonAsync(
            $"/api/orgs/{org}/policygroups/prod/batch",
            new[] { new { addPolicyPack = new { name = "sec" } } });
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        using var scope = app.Services.CreateScope();
        var group = scope.ServiceProvider.GetRequiredService<IPolicyStore>().GetGroup(org, "prod");
        Assert.Contains("sec", group!.AppliedPolicyPacks);
    }

    [Fact]
    public async Task BatchUpdateMissingGroupIs404()
    {
        using var client = app.CreateClient();
        using var resp = await client.PatchAsJsonAsync(
            $"/api/orgs/{NewOrg()}/policygroups/ghost/batch",
            new[] { new { addPolicyPack = new { name = "sec" } } });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task StackPolicyGroupsReturnsGroupForMatchingStack()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        SeedGroupRow(org, "prod", stacks: new() { "webapp/dev" }, packs: new() { "sec" });

        var resp = await client.GetFromJsonAsync<AppListPolicyGroupsResponse>(
            $"/api/stacks/{org}/webapp/dev/policygroups");

        Assert.Contains(resp!.PolicyGroups, g => g.Name == "prod");
    }

    [Fact]
    public async Task PolicyGroupMetadataCountsStacks()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        SeedGroupRow(org, "prod", stacks: new() { "webapp/dev" }, packs: new() { "sec" });

        var meta = await client.GetFromJsonAsync<PolicyGroupMetadata>($"/api/orgs/{org}/policygroups/metadata");

        Assert.Equal(1, meta!.TotalStacks);
        Assert.Equal(1, meta.ProtectedStacks); // the group has an applied pack
    }

    [Fact]
    public async Task RegistryPolicyPackReturnsSeededPack()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        SeedPack(org, "sec", "Security");

        var resp = await client.GetFromJsonAsync<GetRegistryPolicyPackVersionResponse>(
            $"/api/orgs/{org}/registry/policypacks/sec");

        Assert.Equal("sec", resp!.PolicyPack.Name);
        Assert.Equal(org, resp.PolicyPack.Publisher);
    }

    [Fact]
    public async Task RegistryPolicyPackMissingIs404()
    {
        using var client = app.CreateClient();
        using var resp = await client.GetAsync($"/api/orgs/{NewOrg()}/registry/policypacks/ghost");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    private void SeedGroup(string org, string name)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPolicyStore>().NewGroup(org, name);
    }

    private void SeedPack(string org, string name, string displayName)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPolicyStore>()
            .CreatePackVersion(org, name, displayName, new List<AppPolicy>(), versionTag: "1.0.0");
    }

    // Stacks can't be attached to a group through the API, so seed the row directly for the read-path tests.
    private void SeedGroupRow(string org, string name, List<string> stacks, List<string> packs)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HappyPumiDbContext>();
        db.PolicyGroups.Add(new PolicyGroupRow { Org = org, Name = name, Stacks = stacks, AppliedPolicyPacks = packs });
        db.SaveChanges();
    }
}
