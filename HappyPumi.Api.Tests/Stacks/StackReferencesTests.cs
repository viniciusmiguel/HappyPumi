using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for stack references: upstream (stacks this one reads via a StackReference resource)
/// and downstream (stacks that read this one), derived from checkpoint StackReference resources.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackReferencesTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";
    private static string Base(string s) => $"/api/stacks/{Org}/{Project}/{s}";

    private static async Task NewStack(HttpClient client, string stack)
        => await client.PostAsJsonAsync($"/api/stacks/{Org}/{Project}", new AppCreateStackRequest { StackName = stack });

    // Completes an update whose checkpoint contains the given resources (each a dict).
    private static async Task RunUpdate(HttpClient client, string stack, object[] resources)
    {
        var created = await (await client.PostAsJsonAsync($"{Base(stack)}/update", new AppUpdateProgramRequest()))
            .Content.ReadFromJsonAsync<AppUpdateProgramResponse>();
        await client.PostAsJsonAsync($"{Base(stack)}/update/{created!.UpdateId}", new AppStartUpdateRequest());
        await client.PatchAsJsonAsync($"{Base(stack)}/update/{created.UpdateId}/checkpoint",
            new AppPatchUpdateCheckpointRequest
            {
                Version = 3,
                Deployment = new Dictionary<string, object?> { ["resources"] = resources },
            });
        await client.PostAsJsonAsync($"{Base(stack)}/update/{created.UpdateId}/complete",
            new AppCompleteUpdateRequest { Status = "succeeded" });
    }

    private static object StackRes(string suffix) => new Dictionary<string, object?>
    {
        ["urn"] = $"urn:pulumi:dev::{Project}::pulumi:pulumi:Stack::{suffix}", ["type"] = "pulumi:pulumi:Stack",
    };

    private static object RefRes(string targetQualified) => new Dictionary<string, object?>
    {
        ["urn"] = $"urn:pulumi:dev::{Project}::pulumi:pulumi:StackReference::ref",
        ["type"] = "pulumi:pulumi:StackReference",
        ["inputs"] = new Dictionary<string, object?> { ["name"] = targetQualified },
    };

    [Fact]
    public async Task UpstreamAndDownstreamReferencesAreDerived()
    {
        using var client = app.CreateClient();
        var producer = $"prod-{Guid.NewGuid():N}";
        var consumer = $"cons-{Guid.NewGuid():N}";
        await NewStack(client, producer);
        await NewStack(client, consumer);
        await RunUpdate(client, producer, new[] { StackRes("p") });
        // consumer reads the producer via a StackReference resource.
        await RunUpdate(client, consumer, new[] { StackRes("c"), RefRes($"{Org}/{Project}/{producer}") });

        var up = await client.GetFromJsonAsync<ListDownstreamStackReferencesResponse>($"{Base(consumer)}/upstreamreferences");
        Assert.NotNull(up);
        Assert.Contains(up!.ReferencedStacks, r => r.Name == producer && r.RoutingProject == Project && r.Organization == Org);

        var down = await client.GetFromJsonAsync<ListDownstreamStackReferencesResponse>($"{Base(producer)}/downstreamreferences");
        Assert.NotNull(down);
        Assert.Contains(down!.ReferencedStacks, r => r.Name == consumer);
    }

    [Fact]
    public async Task NoReferencesReturnsEmpty()
    {
        using var client = app.CreateClient();
        var solo = $"solo-{Guid.NewGuid():N}";
        await NewStack(client, solo);
        await RunUpdate(client, solo, new[] { StackRes("s") });

        var up = await client.GetFromJsonAsync<ListDownstreamStackReferencesResponse>($"{Base(solo)}/upstreamreferences");
        Assert.NotNull(up);
        Assert.Empty(up!.ReferencedStacks);
    }
}
