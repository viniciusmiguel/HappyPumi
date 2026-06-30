using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for update + preview detail reads (PR2): update/preview summaries, the update
/// timeline, preview history, and persisted engine events. Real updates and previews are driven through
/// the lifecycle so the stores hold the records these endpoints project.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackUpdateDetailTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";
    private const string StackUrn = "urn:pulumi:dev::webapp::pulumi:pulumi:Stack::webapp-dev";
    private static string Base(string s) => $"/api/stacks/{Org}/{Project}/{s}";
    private static string Console(string s) => $"/api/console/stacks/{Org}/{Project}/{s}";

    // Creates a stack and runs one update to completion; returns (stack, updateId). Stack version becomes 1.
    private static async Task<(string Stack, string UpdateId)> NewStackWithUpdate(HttpClient client)
    {
        var stack = $"upd-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync($"/api/stacks/{Org}/{Project}", new AppCreateStackRequest { StackName = stack });
        var created = await (await client.PostAsJsonAsync($"{Base(stack)}/update", new AppUpdateProgramRequest()))
            .Content.ReadFromJsonAsync<AppUpdateProgramResponse>();
        await client.PostAsJsonAsync($"{Base(stack)}/update/{created!.UpdateId}", new AppStartUpdateRequest());
        var deployment = new Dictionary<string, object?>
        {
            ["manifest"] = new Dictionary<string, object?>(),
            ["resources"] = new[]
            {
                new Dictionary<string, object?> { ["urn"] = StackUrn, ["type"] = "pulumi:pulumi:Stack", ["custom"] = false },
            },
        };
        await client.PatchAsJsonAsync($"{Base(stack)}/update/{created.UpdateId}/checkpoint",
            new AppPatchUpdateCheckpointRequest { Version = 1, Deployment = deployment });
        await client.PostAsJsonAsync($"{Base(stack)}/update/{created.UpdateId}/complete",
            new AppCompleteUpdateRequest { Status = "succeeded" });
        return (stack, created.UpdateId);
    }

    // Creates and completes a dry-run preview on an existing stack; returns its update id.
    private static async Task<string> RunPreview(HttpClient client, string stack)
    {
        var created = await (await client.PostAsJsonAsync($"{Base(stack)}/preview", new AppUpdateProgramRequest()))
            .Content.ReadFromJsonAsync<AppUpdateProgramResponse>();
        await client.PostAsJsonAsync($"{Base(stack)}/preview/{created!.UpdateId}", new AppStartUpdateRequest());
        await client.PostAsJsonAsync($"{Base(stack)}/preview/{created.UpdateId}/complete",
            new AppCompleteUpdateRequest { Status = "succeeded" });
        return created.UpdateId;
    }

    [Fact]
    public async Task UpdateSummaryByVersionReturnsResultAndResourceCount()
    {
        using var client = app.CreateClient();
        var (stack, _) = await NewStackWithUpdate(client);

        var resp = await client.GetFromJsonAsync<UpdateSummary>($"{Base(stack)}/updates/1/summary");

        Assert.NotNull(resp);
        Assert.Equal("succeeded", resp!.Result);
        Assert.Equal(1, resp.ResourceCount);
    }

    [Fact]
    public async Task UpdateSummaryUnknownVersionReturns404()
    {
        using var client = app.CreateClient();
        var (stack, _) = await NewStackWithUpdate(client);
        using var r = await client.GetAsync($"{Base(stack)}/updates/99/summary");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task ConsoleUpdateSummaryByIdReturnsHumanText()
    {
        using var client = app.CreateClient();
        var (stack, updateId) = await NewStackWithUpdate(client);

        var resp = await client.GetFromJsonAsync<ConsoleUpdateSummary>($"{Console(stack)}/updates/{updateId}/summary");

        Assert.NotNull(resp);
        Assert.Contains("update #1", resp!.Summary);
        Assert.Contains("succeeded", resp.Summary);
    }

    [Fact]
    public async Task ConsoleLatestUpdateSummaryReturnsHumanText()
    {
        using var client = app.CreateClient();
        var (stack, _) = await NewStackWithUpdate(client);

        var resp = await client.GetFromJsonAsync<ConsoleUpdateSummary>($"{Console(stack)}/updates/latest/summary");

        Assert.NotNull(resp);
        Assert.Contains("update #1", resp!.Summary);
    }

    [Fact]
    public async Task UpdateTimelineByVersionReturnsFocalUpdate()
    {
        using var client = app.CreateClient();
        var (stack, _) = await NewStackWithUpdate(client);

        var resp = await client.GetFromJsonAsync<GetUpdateTimelineResponse>($"{Base(stack)}/updates/1/timeline");

        Assert.NotNull(resp);
        Assert.Equal(1, resp!.Update.Version);
        Assert.Null(resp.CollatedPullRequest);
        Assert.Empty(resp.CollatedUpdateEvents);
    }

    [Fact]
    public async Task UpdateTimelineUnknownVersionReturns404()
    {
        using var client = app.CreateClient();
        var (stack, _) = await NewStackWithUpdate(client);
        using var r = await client.GetAsync($"{Base(stack)}/updates/99/timeline");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task LatestTimelineIncludesPreviewsAsPreviews()
    {
        using var client = app.CreateClient();
        var (stack, _) = await NewStackWithUpdate(client);
        var previewId = await RunPreview(client, stack);

        var resp = await client.GetFromJsonAsync<GetUpdateTimelineResponse>($"{Base(stack)}/updates/latest/timeline");

        Assert.NotNull(resp);
        Assert.Contains(resp!.Previews, p => p.UpdateId == previewId);
    }

    [Fact]
    public async Task EngineEventsAreaPersistedAndReplayed()
    {
        using var client = app.CreateClient();
        var (stack, updateId) = await NewStackWithUpdate(client);

        var single = new AppEngineEvent
        {
            Timestamp = 100,
            ResourcePreEvent = new AppResourcePreEvent { Metadata = new AppStepEventMetadata { Urn = StackUrn, Op = "create" } },
        };
        await client.PostAsJsonAsync($"{Base(stack)}/update/{updateId}/events", single);

        var batch = new AppEngineEventBatch { Events = new List<AppEngineEvent> { new() { Timestamp = 200, StdoutEvent = new AppStdoutEngineEvent { Message = "done" } } } };
        await client.PostAsJsonAsync($"{Base(stack)}/update/{updateId}/events/batch", batch);

        var resp = await client.GetFromJsonAsync<GetUpdateEventsResponse>($"{Base(stack)}/update/{updateId}/events");

        Assert.NotNull(resp);
        Assert.Equal(2, resp!.Events.Count);
        Assert.Equal("resourcePreEvent", resp.Events[0].Type);
        Assert.Equal("stdoutEvent", resp.Events[1].Type);
        Assert.Equal(string.Empty, resp.ContinuationToken);
    }

    [Fact]
    public async Task PreviewHistoryListsCompletedPreviews()
    {
        using var client = app.CreateClient();
        var (stack, _) = await NewStackWithUpdate(client);
        var previewId = await RunPreview(client, stack);

        var resp = await client.GetFromJsonAsync<GetStackUpdatesResponse>($"{Base(stack)}/updates/latest/previews");

        Assert.NotNull(resp);
        Assert.Contains(resp!.Updates, u => u.UpdateId == previewId);
        Assert.Equal(resp.Updates.Count, resp.Total);
    }

    [Fact]
    public async Task SinglePreviewReturnsUpdateInfo()
    {
        using var client = app.CreateClient();
        var (stack, _) = await NewStackWithUpdate(client);
        var previewId = await RunPreview(client, stack);

        var resp = await client.GetFromJsonAsync<UpdateInfo>($"{Base(stack)}/previews/{previewId}");

        Assert.NotNull(resp);
        Assert.Equal(previewId, resp!.UpdateId);
        Assert.Equal("preview", resp.Info.Kind);
    }

    [Fact]
    public async Task SinglePreviewForNonPreviewUpdateReturns404()
    {
        using var client = app.CreateClient();
        var (stack, updateId) = await NewStackWithUpdate(client); // a real update, not a preview
        using var r = await client.GetAsync($"{Base(stack)}/previews/{updateId}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task PreviewSummaryReturnsResultAndCount()
    {
        using var client = app.CreateClient();
        var (stack, _) = await NewStackWithUpdate(client);
        var previewId = await RunPreview(client, stack);

        var resp = await client.GetFromJsonAsync<UpdateSummary>($"{Base(stack)}/previews/{previewId}/summary");

        Assert.NotNull(resp);
        Assert.Equal("succeeded", resp!.Result);
        Assert.Equal(0, resp.ResourceCount); // preview engine events are not yet persisted (follow-up)
    }

    [Fact]
    public async Task PreviewSummaryUnknownUpdateReturns404()
    {
        using var client = app.CreateClient();
        var (stack, _) = await NewStackWithUpdate(client);
        using var r = await client.GetAsync($"{Base(stack)}/previews/{Guid.NewGuid():N}/summary");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }
}
