using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for the stack activity feed: the stack's updates projected into a paginated,
/// newest-first activity list.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackActivityTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";
    private static string Base(string s) => $"/api/stacks/{Org}/{Project}/{s}";

    private static async Task<string> NewStack(HttpClient client)
    {
        var stack = $"act-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync($"/api/stacks/{Org}/{Project}", new AppCreateStackRequest { StackName = stack });
        return stack;
    }

    private static async Task RunUpdate(HttpClient client, string stack)
    {
        var created = await (await client.PostAsJsonAsync($"{Base(stack)}/update", new AppUpdateProgramRequest()))
            .Content.ReadFromJsonAsync<AppUpdateProgramResponse>();
        await client.PostAsJsonAsync($"{Base(stack)}/update/{created!.UpdateId}", new AppStartUpdateRequest());
        await client.PatchAsJsonAsync($"{Base(stack)}/update/{created.UpdateId}/checkpoint",
            new AppPatchUpdateCheckpointRequest { Version = 3, Deployment = new Dictionary<string, object?>() });
        await client.PostAsJsonAsync($"{Base(stack)}/update/{created.UpdateId}/complete",
            new AppCompleteUpdateRequest { Status = "succeeded" });
    }

    [Fact]
    public async Task ActivityListsUpdatesNewestFirst()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        await RunUpdate(client, stack);
        await RunUpdate(client, stack);

        var resp = await client.GetFromJsonAsync<GetStackActivityResponse>($"{Base(stack)}/activity");

        Assert.NotNull(resp);
        Assert.Equal(2, resp!.Total);
        Assert.Equal(2, resp.Activity.Count);
        Assert.NotNull(resp.Activity[0].Update);
        // newest first
        Assert.True(resp.Activity[0].Update!.Version >= resp.Activity[1].Update!.Version);
    }

    [Fact]
    public async Task ActivityPaginates()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        await RunUpdate(client, stack);
        await RunUpdate(client, stack);

        var resp = await client.GetFromJsonAsync<GetStackActivityResponse>($"{Base(stack)}/activity?pageSize=1&page=1");

        Assert.NotNull(resp);
        Assert.Equal(2, resp!.Total);
        Assert.Single(resp.Activity);
        Assert.Equal(1, resp.ItemsPerPage);
    }

    [Fact]
    public async Task ActivityUnknownStackReturns404()
    {
        using var client = app.CreateClient();
        using var r = await client.GetAsync($"{Base("missing-" + Guid.NewGuid().ToString("N"))}/activity");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }
}
