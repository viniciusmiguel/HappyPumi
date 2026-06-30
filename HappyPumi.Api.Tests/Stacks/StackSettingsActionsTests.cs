using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for the Stack Detail "Settings actions" endpoints (PR6): single-tag edit, export at a
/// historical version, notification settings, ownership reassignment, transfer, annotations, and the
/// starter-workflow generator.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackSettingsActionsTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";
    private static string Base(string s) => $"/api/stacks/{Org}/{Project}/{s}";

    private static async Task<string> NewStack(HttpClient client)
    {
        var stack = $"set-{Guid.NewGuid():N}";
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

    // ── UpdateStackTag ─────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateTagChangesAnExistingTagValue()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        await client.PostAsJsonAsync($"{Base(stack)}/tags", new StackTag { Name = "env", Value = "dev" });

        using var resp = await client.PatchAsJsonAsync($"{Base(stack)}/tags/env", new StackTag { Name = "env", Value = "prod" });

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        var meta = await client.GetFromJsonAsync<StackMetadata>($"{Base(stack)}/metadata");
        Assert.Equal("prod", meta!.Tags["env"]);
    }

    [Fact]
    public async Task UpdateMissingTagReturns404()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        using var resp = await client.PatchAsJsonAsync($"{Base(stack)}/tags/ghost", new StackTag { Name = "ghost", Value = "x" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── ExportStackAtVersion ───────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ExportAtCompletedVersionReturnsItsCheckpoint()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        await RunUpdate(client, stack);

        var dep = await client.GetFromJsonAsync<AppUntypedDeployment>($"{Base(stack)}/export/1");

        Assert.NotNull(dep);
        Assert.Equal(3, dep!.Version);
    }

    [Fact]
    public async Task ExportAtCurrentVersionOfFreshStackIsEmptyDeployment()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);

        var dep = await client.GetFromJsonAsync<AppUntypedDeployment>($"{Base(stack)}/export/0");

        Assert.NotNull(dep);
        Assert.Equal(3, dep!.Version); // DeploymentFactory.Empty()
    }

    [Fact]
    public async Task ExportAtUnknownVersionReturns404()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        using var resp = await client.GetAsync($"{Base(stack)}/export/99");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task ExportUnknownStackReturns404()
    {
        using var client = app.CreateClient();
        using var resp = await client.GetAsync($"{Base("missing-" + Guid.NewGuid().ToString("N"))}/export/1");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── UpdateStackNotificationSettings ────────────────────────────────────────────────────────────
    [Fact]
    public async Task NotificationSettingsArePersistedAndReturned()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);

        var resp = await client.PatchAsJsonAsync($"{Base(stack)}/notifications/settings",
            new UpdateStackNotificationSettingsRequest { NotifyUpdateFailure = true, NotifyUpdateSuccess = false });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var meta = await resp.Content.ReadFromJsonAsync<StackMetadata>();
        Assert.True(meta!.NotificationSettings.NotifyUpdateFailure);
        Assert.False(meta.NotificationSettings.NotifyUpdateSuccess);

        var reread = await client.GetFromJsonAsync<StackMetadata>($"{Base(stack)}/metadata");
        Assert.True(reread!.NotificationSettings.NotifyUpdateFailure);
    }

    [Fact]
    public async Task NotificationSettingsUnknownStackReturns404()
    {
        using var client = app.CreateClient();
        using var resp = await client.PatchAsJsonAsync($"{Base("missing-" + Guid.NewGuid().ToString("N"))}/notifications/settings",
            new UpdateStackNotificationSettingsRequest { NotifyUpdateSuccess = true });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── ReassignStackOwnership ─────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ReassignOwnershipSetsAndReturnsTheNewOwner()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);

        var resp = await client.PostAsJsonAsync($"{Base(stack)}/ownership",
            new UserInfo { GithubLogin = "alice", Name = "Alice" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var owner = await resp.Content.ReadFromJsonAsync<UserInfo>();
        Assert.Equal("alice", owner!.GithubLogin);

        var meta = await client.GetFromJsonAsync<StackMetadata>($"{Base(stack)}/metadata");
        Assert.Equal("alice", meta!.OwnedBy.GithubLogin);
    }

    [Fact]
    public async Task ReassignOwnershipUnknownStackReturns404()
    {
        using var client = app.CreateClient();
        using var resp = await client.PostAsJsonAsync($"{Base("missing-" + Guid.NewGuid().ToString("N"))}/ownership",
            new UserInfo { GithubLogin = "alice" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    // ── TransferStack ──────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task TransferMovesTheStackToAnotherOrg()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        var destOrg = $"dest-{Guid.NewGuid():N}";

        using var resp = await client.PostAsJsonAsync($"{Base(stack)}/transfer",
            new TransferStackRequest { ToOrg = destOrg });

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        using var atDest = await client.GetAsync($"/api/stacks/{destOrg}/{Project}/{stack}/metadata");
        Assert.Equal(HttpStatusCode.OK, atDest.StatusCode);
        using var atSource = await client.GetAsync($"{Base(stack)}/metadata");
        Assert.Equal(HttpStatusCode.NotFound, atSource.StatusCode);
    }

    [Fact]
    public async Task TransferWithoutDestinationOrgIs400()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        using var resp = await client.PostAsJsonAsync($"{Base(stack)}/transfer", new TransferStackRequest { ToOrg = "" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task TransferToAnOccupiedDestinationIs409()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        var destOrg = $"dest-{Guid.NewGuid():N}";
        // Seed a colliding stack at the destination by creating it directly there.
        await client.PostAsJsonAsync($"/api/stacks/{destOrg}/{Project}", new AppCreateStackRequest { StackName = stack });

        using var resp = await client.PostAsJsonAsync($"{Base(stack)}/transfer", new TransferStackRequest { ToOrg = destOrg });
        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    // ── Annotations ────────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AnnotationGetIsEmptyObjectWhenUnset()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);

        using var resp = await client.GetAsync($"{Base(stack)}/annotations/compliance");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Object, json.ValueKind);
        Assert.Empty(json.EnumerateObject());
    }

    [Fact]
    public async Task AnnotationUpsertThenGetRoundTripsThePayload()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);

        using var put = await client.PatchAsJsonAsync($"{Base(stack)}/annotations/compliance", new { owner = "platform", level = 3 });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var json = await client.GetFromJsonAsync<JsonElement>($"{Base(stack)}/annotations/compliance");
        Assert.Equal("platform", json.GetProperty("owner").GetString());
        Assert.Equal(3, json.GetProperty("level").GetInt32());
    }

    // ── GetStackStarterWorkflow ────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task StarterWorkflowReturnsGitHubActionsYaml()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);

        var resp = await client.PostAsJsonAsync($"{Base(stack)}/workflow",
            new GetStarterWorkflowRequest { CiSystem = "github", WorkingDirectory = "infra" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<GetStackStarterWorkflowResponse>();
        Assert.Contains("pulumi/actions", body!.Content);
        Assert.Contains($"{Org}/{Project}/{stack}", body.Content);
        Assert.Contains("work-dir: infra", body.Content);
    }

    [Fact]
    public async Task StarterWorkflowRejectsUnsupportedCiSystem()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        using var resp = await client.PostAsJsonAsync($"{Base(stack)}/workflow",
            new GetStarterWorkflowRequest { CiSystem = "gitlab" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
