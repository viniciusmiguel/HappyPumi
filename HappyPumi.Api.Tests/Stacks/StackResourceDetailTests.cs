using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for stack resource-detail reads: resources by version, a single resource by URN
/// (latest and at a version). A real update is run to completion so the per-version checkpoint exists.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackResourceDetailTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";
    private static string Base(string s) => $"/api/stacks/{Org}/{Project}/{s}";

    // A URN the checkpoint we write always contains.
    private const string StackUrn = "urn:pulumi:dev::webapp::pulumi:pulumi:Stack::webapp-dev";

    private static async Task<string> NewStackWithUpdate(HttpClient client)
    {
        var stack = $"res-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync($"/api/stacks/{Org}/{Project}", new AppCreateStackRequest { StackName = stack });
        var created = await (await client.PostAsJsonAsync($"{Base(stack)}/update", new AppUpdateProgramRequest()))
            .Content.ReadFromJsonAsync<AppUpdateProgramResponse>();
        await client.PostAsJsonAsync($"{Base(stack)}/update/{created!.UpdateId}", new AppStartUpdateRequest());
        var deployment = new Dictionary<string, object?>
        {
            ["manifest"] = new Dictionary<string, object?>(),
            ["resources"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["urn"] = StackUrn, ["type"] = "pulumi:pulumi:Stack", ["custom"] = false,
                },
            },
        };
        await client.PatchAsJsonAsync($"{Base(stack)}/update/{created.UpdateId}/checkpoint",
            new AppPatchUpdateCheckpointRequest { Version = 3, Deployment = deployment });
        await client.PostAsJsonAsync($"{Base(stack)}/update/{created.UpdateId}/complete",
            new AppCompleteUpdateRequest { Status = "succeeded" });
        return stack;
    }

    [Fact]
    public async Task LatestSingleResourceReturnsByUrn()
    {
        using var client = app.CreateClient();
        var stack = await NewStackWithUpdate(client);

        var resp = await client.GetFromJsonAsync<GetStackResourceResponse>(
            $"{Base(stack)}/resources/latest/{Uri.EscapeDataString(StackUrn)}");

        Assert.NotNull(resp);
        Assert.Equal(StackUrn, resp!.Resource.Resource.Urn);
    }

    [Fact]
    public async Task LatestSingleResourceUnknownUrnReturns404()
    {
        using var client = app.CreateClient();
        var stack = await NewStackWithUpdate(client);
        using var r = await client.GetAsync($"{Base(stack)}/resources/latest/{Uri.EscapeDataString("urn:nope")}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task ResourcesByVersionReturnsCheckpointResources()
    {
        using var client = app.CreateClient();
        var stack = await NewStackWithUpdate(client);

        var resp = await client.GetFromJsonAsync<GetStackResourcesResponse>($"{Base(stack)}/resources/1");

        Assert.NotNull(resp);
        Assert.Equal(1, resp!.Version);
        Assert.Contains(resp.Resources, ri => ri.Resource.Urn == StackUrn);
    }

    [Fact]
    public async Task ResourcesByUnknownVersionReturns404()
    {
        using var client = app.CreateClient();
        var stack = await NewStackWithUpdate(client);
        using var r = await client.GetAsync($"{Base(stack)}/resources/99");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    // Runs one more update whose checkpoint contains the given resource URNs; bumps the stack version.
    private static async Task RunUpdate(HttpClient client, string stack, params string[] urns)
    {
        var created = await (await client.PostAsJsonAsync($"{Base(stack)}/update", new AppUpdateProgramRequest()))
            .Content.ReadFromJsonAsync<AppUpdateProgramResponse>();
        await client.PostAsJsonAsync($"{Base(stack)}/update/{created!.UpdateId}", new AppStartUpdateRequest());
        var deployment = new Dictionary<string, object?>
        {
            ["manifest"] = new Dictionary<string, object?>(),
            ["resources"] = urns.Select(u => new Dictionary<string, object?>
            {
                ["urn"] = u, ["type"] = "pulumi:pulumi:Stack", ["custom"] = false,
            }).ToArray(),
        };
        await client.PatchAsJsonAsync($"{Base(stack)}/update/{created.UpdateId}/checkpoint",
            new AppPatchUpdateCheckpointRequest { Version = 3, Deployment = deployment });
        await client.PostAsJsonAsync($"{Base(stack)}/update/{created.UpdateId}/complete",
            new AppCompleteUpdateRequest { Status = "succeeded" });
    }

    [Fact]
    public async Task ResourcesAtHistoricalVersionUsesUpdateCheckpoint()
    {
        using var client = app.CreateClient();
        var stack = await NewStackWithUpdate(client);            // v1: [StackUrn]
        const string extraUrn = "urn:pulumi:dev::webapp::pulumi:pulumi:Stack::webapp-dev-2";
        await RunUpdate(client, stack, StackUrn, extraUrn);      // v2: [StackUrn, extraUrn]

        var v1 = await client.GetFromJsonAsync<GetStackResourcesResponse>($"{Base(stack)}/resources/1");
        var v2 = await client.GetFromJsonAsync<GetStackResourcesResponse>($"{Base(stack)}/resources/2");

        // v1 is served from the historical update checkpoint (not the latest), so it lacks the v2 resource.
        Assert.DoesNotContain(v1!.Resources, ri => ri.Resource.Urn == extraUrn);
        Assert.Contains(v2!.Resources, ri => ri.Resource.Urn == extraUrn);
    }

    [Fact]
    public async Task SingleResourceAtVersionReturnsByUrn()
    {
        using var client = app.CreateClient();
        var stack = await NewStackWithUpdate(client);

        var resp = await client.GetFromJsonAsync<GetStackResourceResponse>(
            $"{Base(stack)}/resources/1/{Uri.EscapeDataString(StackUrn)}");

        Assert.NotNull(resp);
        Assert.Equal(1, resp!.Version);
        Assert.Equal(StackUrn, resp.Resource.Resource.Urn);
    }
}
