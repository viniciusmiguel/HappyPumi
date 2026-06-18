using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for the Tier-1c update lifecycle (ENDPOINTS.md) over the full HTTP pipeline, driving
/// the same create -> start -> checkpoint -> complete sequence a real `pulumi up` does and asserting the
/// checkpoint is promoted into the stack's exported state.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class UpdateLifecycleEndpointTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";

    private static string StackPath(string stack) => $"/api/stacks/{Org}/{Project}/{stack}";

    private static async Task<string> NewStack(HttpClient client)
    {
        var stack = $"up-{Guid.NewGuid():N}";
        using var _ = await client.PostAsJsonAsync(
            $"/api/stacks/{Org}/{Project}", new AppCreateStackRequest { StackName = stack });
        return stack;
    }

    private static AppPatchUpdateCheckpointRequest Checkpoint() => new()
    {
        Version = 3,
        Deployment = new Dictionary<string, object?> { ["manifest"] = new Dictionary<string, object?>() },
    };

    [Fact]
    public async Task FullUpdateLifecyclePromotesCheckpointAndBumpsVersion()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);

        var created = await Post<AppUpdateProgramResponse>(client, $"{StackPath(stack)}/update", new AppUpdateProgramRequest());
        var updateId = created.UpdateId;
        Assert.False(string.IsNullOrEmpty(updateId));

        var started = await Post<AppStartUpdateResponse>(client, $"{StackPath(stack)}/update/{updateId}", new AppStartUpdateRequest());
        Assert.Equal(1, started.Version);            // fresh stack (v0) -> this update produces v1
        Assert.Equal(0, started.JournalVersion);     // journaling disabled -> checkpoint path
        Assert.False(string.IsNullOrEmpty(started.Token));

        using var checkpoint = await client.PatchAsJsonAsync($"{StackPath(stack)}/update/{updateId}/checkpoint", Checkpoint());
        Assert.Equal(HttpStatusCode.NoContent, checkpoint.StatusCode);

        using var completed = await client.PostAsJsonAsync(
            $"{StackPath(stack)}/update/{updateId}/complete", new AppCompleteUpdateRequest { Status = "succeeded" });
        Assert.Equal(HttpStatusCode.NoContent, completed.StatusCode);

        // The promoted checkpoint is now the stack's exported state, and the version was bumped.
        var exported = await client.GetFromJsonAsync<AppUntypedDeployment>($"{StackPath(stack)}/export");
        Assert.Equal(3, exported!.Version);
        var fetched = await client.GetFromJsonAsync<AppStack>(StackPath(stack));
        Assert.Equal(1, fetched!.Version);
    }

    // Preview is a dry run: completing it succeeds but must not change stack state or version.
    [Fact]
    public async Task PreviewDoesNotPromoteStateOrBumpVersion()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);

        var created = await Post<AppUpdateProgramResponse>(client, $"{StackPath(stack)}/preview", new AppUpdateProgramRequest());
        await Post<AppStartUpdateResponse>(client, $"{StackPath(stack)}/preview/{created.UpdateId}", new AppStartUpdateRequest());
        using var completed = await client.PostAsJsonAsync(
            $"{StackPath(stack)}/preview/{created.UpdateId}/complete", new AppCompleteUpdateRequest { Status = "succeeded" });
        Assert.Equal(HttpStatusCode.NoContent, completed.StatusCode);

        var fetched = await client.GetFromJsonAsync<AppStack>(StackPath(stack));
        Assert.Equal(0, fetched!.Version);
        using var export = await client.GetAsync($"{StackPath(stack)}/export");
        var deployment = await export.Content.ReadFromJsonAsync<AppUntypedDeployment>();
        Assert.Equal(3, deployment!.Version); // still the empty base, never updated
    }

    [Fact]
    public async Task CreateUpdateForUnknownStackReturns404()
    {
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"{StackPath($"ghost-{Guid.NewGuid():N}")}/update", new AppUpdateProgramRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteWithUnknownStatusReturns400()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        var created = await Post<AppUpdateProgramResponse>(client, $"{StackPath(stack)}/update", new AppUpdateProgramRequest());
        await Post<AppStartUpdateResponse>(client, $"{StackPath(stack)}/update/{created.UpdateId}", new AppStartUpdateRequest());

        using var response = await client.PostAsJsonAsync(
            $"{StackPath(stack)}/update/{created.UpdateId}/complete", new AppCompleteUpdateRequest { Status = "banana" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUpdateStatusReportsRunningAfterStart()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        var created = await Post<AppUpdateProgramResponse>(client, $"{StackPath(stack)}/update", new AppUpdateProgramRequest());
        await Post<AppStartUpdateResponse>(client, $"{StackPath(stack)}/update/{created.UpdateId}", new AppStartUpdateRequest());

        var status = await client.GetFromJsonAsync<AppUpdateResults>($"{StackPath(stack)}/update/{created.UpdateId}");

        Assert.Equal("running", status!.Status);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
