using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for the stack webhook endpoints (get/update/delete/deliveries/ping/redeliver) and the
/// fire-on-update-completion wiring. Webhooks point at an unreachable URL, so the dispatcher records a failed
/// delivery (responseCode 0) rather than throwing — proving delivery is recorded and firing never faults.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackWebhookTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webhooks";
    private const string DeadUrl = "http://127.0.0.1:1/hook";

    private static string Hooks(string stack) => $"/api/stacks/{Org}/{Project}/{stack}/hooks";

    private static async Task<string> NewStack(HttpClient client)
    {
        var stack = $"wh-{Guid.NewGuid():N}";
        (await client.PostAsJsonAsync($"/api/stacks/{Org}/{Project}", new AppCreateStackRequest { StackName = stack }))
            .EnsureSuccessStatusCode();
        return stack;
    }

    private static async Task CreateHook(HttpClient client, string stack, string name, string url = DeadUrl)
        => (await client.PostAsJsonAsync(Hooks(stack), new Webhook { Name = name, PayloadUrl = url, Active = true }))
            .EnsureSuccessStatusCode();

    private static StringContent Empty() => new("{}", Encoding.UTF8, "application/json");

    [Fact]
    public async Task CreateGetUpdateDeleteLifecycle()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        await CreateHook(client, stack, "ci");

        var got = await client.GetFromJsonAsync<WebhookResponse>($"{Hooks(stack)}/ci");
        Assert.Equal("ci", got!.Name);

        using var patch = await client.PatchAsJsonAsync($"{Hooks(stack)}/ci",
            new Webhook { Active = false, DisplayName = "CI hook", Secret = "topsecret" });
        var updated = await patch.Content.ReadFromJsonAsync<WebhookResponse>();
        Assert.False(updated!.Active);
        Assert.Equal("CI hook", updated.DisplayName);
        Assert.True(updated.HasSecret);
        Assert.Null(updated.Secret); // never echoed

        using var del = await client.DeleteAsync($"{Hooks(stack)}/ci");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        using var gone = await client.GetAsync($"{Hooks(stack)}/ci");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task GetMissingWebhookReturns404()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        using var res = await client.GetAsync($"{Hooks(stack)}/nope");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task PingRecordsDeliveryAndRedeliverResends()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        await CreateHook(client, stack, "ci");

        var ping = await (await client.PostAsync($"{Hooks(stack)}/ci/ping", Empty()))
            .Content.ReadFromJsonAsync<WebhookDelivery>();
        Assert.Equal("ping", ping!.Kind);
        Assert.Equal(0, ping.ResponseCode); // connection refused, captured not thrown

        var deliveries = await client.GetFromJsonAsync<List<WebhookDelivery>>($"{Hooks(stack)}/ci/deliveries");
        Assert.Contains(deliveries!, d => d.Kind == "ping");

        using var redeliver = await client.PostAsync($"{Hooks(stack)}/ci/deliveries/ping/redeliver", Empty());
        redeliver.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PingMissingWebhookReturns404()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        using var res = await client.PostAsync($"{Hooks(stack)}/none/ping", Empty());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task RedeliverUnknownEventReturns404()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        await CreateHook(client, stack, "ci");
        using var res = await client.PostAsync($"{Hooks(stack)}/ci/deliveries/stack_update/redeliver", Empty());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CompletingAnUpdateRecordsStackUpdateDelivery()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        await CreateHook(client, stack, "ci");

        var stackPath = $"/api/stacks/{Org}/{Project}/{stack}";
        var created = await (await client.PostAsJsonAsync($"{stackPath}/update", new AppUpdateProgramRequest()))
            .Content.ReadFromJsonAsync<AppUpdateProgramResponse>();
        await client.PostAsJsonAsync($"{stackPath}/update/{created!.UpdateId}", new AppStartUpdateRequest());
        (await client.PostAsJsonAsync($"{stackPath}/update/{created.UpdateId}/complete",
            new AppCompleteUpdateRequest { Status = "succeeded" })).EnsureSuccessStatusCode();

        var deliveries = await client.GetFromJsonAsync<List<WebhookDelivery>>($"{Hooks(stack)}/ci/deliveries");
        Assert.Contains(deliveries!, d => d.Kind == "stack_update");
    }
}
