using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the organization webhook endpoints (list/create/get/update/delete/deliveries/ping/
/// redeliver) and the fire-on-stack-update wiring at org scope. Webhooks point at an unreachable URL, so the
/// dispatcher records a failed delivery (responseCode 0) rather than throwing — proving delivery is recorded
/// and firing never faults the triggering update.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class OrgWebhookTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string DeadUrl = "http://127.0.0.1:1/hook";

    private static string Hooks => $"/api/orgs/{Org}/hooks";
    private static StringContent Empty() => new("{}", Encoding.UTF8, "application/json");

    private static async Task<string> CreateHook(HttpClient client, string url = DeadUrl)
    {
        var name = $"wh-{Guid.NewGuid():N}";
        (await client.PostAsJsonAsync(Hooks, new Webhook { Name = name, PayloadUrl = url, Active = true }))
            .EnsureSuccessStatusCode();
        return name;
    }

    [Fact]
    public async Task CreateGetUpdateDeleteLifecycle()
    {
        using var client = app.CreateClient();
        var name = await CreateHook(client);

        var got = await client.GetFromJsonAsync<WebhookResponse>($"{Hooks}/{name}");
        Assert.Equal(name, got!.Name);

        using var patch = await client.PatchAsJsonAsync($"{Hooks}/{name}",
            new Webhook { Active = false, DisplayName = "Org hook", Secret = "topsecret" });
        var updated = await patch.Content.ReadFromJsonAsync<WebhookResponse>();
        Assert.False(updated!.Active);
        Assert.Equal("Org hook", updated.DisplayName);
        Assert.True(updated.HasSecret);
        Assert.Null(updated.Secret); // never echoed

        var listed = await client.GetFromJsonAsync<List<WebhookResponse>>(Hooks);
        Assert.Contains(listed!, w => w.Name == name);

        using var del = await client.DeleteAsync($"{Hooks}/{name}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        using var gone = await client.GetAsync($"{Hooks}/{name}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task CreateDuplicateNameReturns409()
    {
        using var client = app.CreateClient();
        var name = await CreateHook(client);
        using var dup = await client.PostAsJsonAsync(Hooks, new Webhook { Name = name, PayloadUrl = DeadUrl });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task GetMissingWebhookReturns404()
    {
        using var client = app.CreateClient();
        using var res = await client.GetAsync($"{Hooks}/nope-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task PingRecordsDeliveryAndRedeliverResends()
    {
        using var client = app.CreateClient();
        var name = await CreateHook(client);

        var ping = await (await client.PostAsync($"{Hooks}/{name}/ping", Empty()))
            .Content.ReadFromJsonAsync<WebhookDelivery>();
        Assert.Equal("ping", ping!.Kind);
        Assert.Equal(0, ping.ResponseCode); // connection refused, captured not thrown

        var deliveries = await client.GetFromJsonAsync<List<WebhookDelivery>>($"{Hooks}/{name}/deliveries");
        Assert.Contains(deliveries!, d => d.Kind == "ping");

        using var redeliver = await client.PostAsync($"{Hooks}/{name}/deliveries/ping/redeliver", Empty());
        redeliver.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PingMissingWebhookReturns404()
    {
        using var client = app.CreateClient();
        using var res = await client.PostAsync($"{Hooks}/none-{Guid.NewGuid():N}/ping", Empty());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task RedeliverUnknownEventReturns404()
    {
        using var client = app.CreateClient();
        var name = await CreateHook(client);
        using var res = await client.PostAsync($"{Hooks}/{name}/deliveries/stack_update/redeliver", Empty());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CompletingAnUpdateFiresOrgWebhook()
    {
        using var client = app.CreateClient();
        var name = await CreateHook(client);

        const string project = "orghooks";
        var stack = $"orghook-{Guid.NewGuid():N}";
        var stackPath = $"/api/stacks/{Org}/{project}/{stack}";
        (await client.PostAsJsonAsync($"/api/stacks/{Org}/{project}", new AppCreateStackRequest { StackName = stack }))
            .EnsureSuccessStatusCode();

        var created = await (await client.PostAsJsonAsync($"{stackPath}/update", new AppUpdateProgramRequest()))
            .Content.ReadFromJsonAsync<AppUpdateProgramResponse>();
        await client.PostAsJsonAsync($"{stackPath}/update/{created!.UpdateId}", new AppStartUpdateRequest());
        (await client.PostAsJsonAsync($"{stackPath}/update/{created.UpdateId}/complete",
            new AppCompleteUpdateRequest { Status = "succeeded" })).EnsureSuccessStatusCode();

        var deliveries = await client.GetFromJsonAsync<List<WebhookDelivery>>($"{Hooks}/{name}/deliveries");
        Assert.Contains(deliveries!, d => d.Kind == "stack_update");
    }
}
