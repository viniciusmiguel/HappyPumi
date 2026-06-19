using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for environment scheduled actions (create/list/read/update/delete/pause/resume).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscScheduleTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-sched";

    [Fact]
    public async Task ScheduleLifecycle()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        var created = await (await client.PostAsJsonAsync($"{EnvUrl(name)}/schedules",
            new { kind = "rotation", scheduleCron = "0 0 * * *" }))
            .Content.ReadFromJsonAsync<ScheduledAction>();
        Assert.False(string.IsNullOrEmpty(created!.Id));
        Assert.Equal("0 0 * * *", created.ScheduleCron);
        Assert.False(created.Paused);
        var id = created.Id;

        var list = await client.GetFromJsonAsync<ListScheduledActionsResponse>($"{EnvUrl(name)}/schedules");
        Assert.Contains(list!.Schedules, s => s.Id == id);

        // Update cron.
        (await client.PatchAsJsonAsync($"{EnvUrl(name)}/schedules/{id}", new { scheduleCron = "0 12 * * *" }))
            .EnsureSuccessStatusCode();
        var read = await client.GetFromJsonAsync<ScheduledAction>($"{EnvUrl(name)}/schedules/{id}");
        Assert.Equal("0 12 * * *", read!.ScheduleCron);

        // Pause / resume.
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{EnvUrl(name)}/schedules/{id}/pause", Empty())).StatusCode);
        Assert.True((await client.GetFromJsonAsync<ScheduledAction>($"{EnvUrl(name)}/schedules/{id}"))!.Paused);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{EnvUrl(name)}/schedules/{id}/resume", Empty())).StatusCode);
        Assert.False((await client.GetFromJsonAsync<ScheduledAction>($"{EnvUrl(name)}/schedules/{id}"))!.Paused);

        // Delete.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{EnvUrl(name)}/schedules/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{EnvUrl(name)}/schedules/{id}")).StatusCode);
    }

    [Fact]
    public async Task CreateForMissingEnvironmentReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.PostAsJsonAsync($"{EnvUrl($"missing-{Guid.NewGuid():N}")}/schedules", new { kind = "rotation" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task PauseUnknownScheduleReturns404()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        using var res = await client.PostAsync($"{EnvUrl(name)}/schedules/bogus/pause", Empty());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private static StringContent Empty() => new("{}", Encoding.UTF8, "application/json");
    private static string EnvUrl(string name) => $"/api/esc/environments/{Org}/{Project}/{name}";

    private static async Task<string> CreateEnv(HttpClient client)
    {
        var name = $"env-{Guid.NewGuid():N}";
        using var res = await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });
        res.EnsureSuccessStatusCode();
        return name;
    }
}
