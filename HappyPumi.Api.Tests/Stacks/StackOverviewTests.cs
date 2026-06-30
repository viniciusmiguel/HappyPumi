using System;
using System.Net;
using System.Net.Http.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for the console stack overview aggregation (resources + tags; referenced stacks
/// populated in a later PR).
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackOverviewTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";

    [Fact]
    public async Task OverviewReturnsResourcesAndTags()
    {
        using var client = app.CreateClient();
        var stack = $"ov-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync($"/api/stacks/{Org}/{Project}", new AppCreateStackRequest { StackName = stack });
        await client.PostAsJsonAsync($"/api/stacks/{Org}/{Project}/{stack}/tags",
            new StackTag { Name = "team", Value = "platform" });

        var resp = await client.GetFromJsonAsync<StackOverviewResponse>(
            $"/api/console/stacks/{Org}/{Project}/{stack}/overview");

        Assert.NotNull(resp);
        Assert.Equal("platform", resp!.Tags["team"]);
        Assert.NotNull(resp.Resources);
        Assert.NotNull(resp.ReferencedStacks); // empty until the stack-references PR
    }

    [Fact]
    public async Task OverviewUnknownStackReturns404()
    {
        using var client = app.CreateClient();
        using var r = await client.GetAsync(
            $"/api/console/stacks/{Org}/{Project}/missing-{Guid.NewGuid():N}/overview");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }
}
