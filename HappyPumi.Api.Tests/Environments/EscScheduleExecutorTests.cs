using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for the scheduled-action executor firing due schedules.</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscScheduleExecutorTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-sched-exec";

    private async Task<int> RunDue()
    {
        using var scope = app.Services.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<ScheduleExecutionService>();
        return await executor.RunDueAsync(DateTime.UtcNow, CancellationToken.None);
    }

    [Fact]
    public async Task DueDeletionScheduleSoftDeletesEnvironment()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        await CreateSchedule(client, name, kind: "deletion");

        await RunDue();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{EnvUrl(name)}/metadata")).StatusCode);
    }

    [Fact]
    public async Task DueRotationScheduleRecordsARotationEvent()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        await CreateSchedule(client, name, kind: "rotation");

        await RunDue();

        var history = await client.GetFromJsonAsync<ListEnvironmentSecretRotationHistoryResponse>($"{EnvUrl(name)}/rotate/history");
        Assert.NotEmpty(history!.Events); // the scheduler ran a rotation pass
    }

    private static async Task CreateSchedule(HttpClient client, string env, string kind)
    {
        var past = DateTime.UtcNow.AddMinutes(-5).ToString("o");
        using var res = await client.PostAsJsonAsync($"{EnvUrl(env)}/schedules", new { kind, scheduleOnce = past });
        res.EnsureSuccessStatusCode();
    }

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
