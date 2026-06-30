using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>
/// Component tests for the legacy <c>/api/preview/environments/.../hooks</c> ESC environment webhook
/// endpoints (list/create/get/update/delete/deliveries/ping/redeliver) and the fire-on-env-update wiring.
/// Webhooks point at an unreachable URL, so the dispatcher records a failed delivery (responseCode 0)
/// rather than throwing — proving the delivery is recorded and firing never faults the env update.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class PreviewEnvWebhookTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string DeadUrl = "http://127.0.0.1:1/hook";

    private static string Hooks(string env) => $"/api/preview/environments/{Org}/{env}/hooks";
    private static StringContent Empty() => new("{}", Encoding.UTF8, "application/json");

    private static string EnvName() => $"env-{Guid.NewGuid():N}";

    private static async Task<string> CreateHook(HttpClient client, string env, string url = DeadUrl)
    {
        var name = $"wh-{Guid.NewGuid():N}";
        (await client.PostAsJsonAsync(Hooks(env), new Webhook { Name = name, PayloadUrl = url, Active = true }))
            .EnsureSuccessStatusCode();
        return name;
    }

    [Fact]
    public async Task CreateGetUpdateDeleteLifecycle()
    {
        using var client = app.CreateAuthedClient();
        var env = EnvName();
        var name = await CreateHook(client, env);

        var got = await client.GetFromJsonAsync<WebhookResponse>($"{Hooks(env)}/{name}");
        Assert.Equal(name, got!.Name);
        Assert.Equal(env, got.EnvName);

        using var patch = await client.PatchAsJsonAsync($"{Hooks(env)}/{name}",
            new Webhook { Name = name, PayloadUrl = DeadUrl, Active = false, DisplayName = "Env hook", Secret = "topsecret" });
        var updated = await patch.Content.ReadFromJsonAsync<WebhookResponse>();
        Assert.False(updated!.Active);
        Assert.Equal("Env hook", updated.DisplayName);
        Assert.True(updated.HasSecret);
        Assert.Null(updated.Secret); // never echoed

        var listed = await client.GetFromJsonAsync<List<WebhookResponse>>(Hooks(env));
        Assert.Contains(listed!, w => w.Name == name);

        using var del = await client.DeleteAsync($"{Hooks(env)}/{name}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        using var gone = await client.GetAsync($"{Hooks(env)}/{name}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task CreateDuplicateNameReturns409()
    {
        using var client = app.CreateAuthedClient();
        var env = EnvName();
        var name = await CreateHook(client, env);
        using var dup = await client.PostAsJsonAsync(Hooks(env), new Webhook { Name = name, PayloadUrl = DeadUrl });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task CreateWithoutPayloadUrlReturns400()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.PostAsJsonAsync(Hooks(EnvName()), new Webhook { Name = "bad" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task GetMissingWebhookReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.GetAsync($"{Hooks(EnvName())}/nope-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task PingRecordsDeliveryAndRedeliverResends()
    {
        using var client = app.CreateAuthedClient();
        var env = EnvName();
        var name = await CreateHook(client, env);

        var ping = await (await client.PostAsync($"{Hooks(env)}/{name}/ping", Empty()))
            .Content.ReadFromJsonAsync<WebhookDelivery>();
        Assert.Equal("ping", ping!.Kind);
        Assert.Equal(0, ping.ResponseCode); // connection refused, captured not thrown

        var deliveries = await client.GetFromJsonAsync<List<WebhookDelivery>>($"{Hooks(env)}/{name}/deliveries");
        Assert.Contains(deliveries!, d => d.Kind == "ping");

        using var redeliver = await client.PostAsync($"{Hooks(env)}/{name}/deliveries/ping/redeliver", Empty());
        redeliver.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PingMissingWebhookReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.PostAsync($"{Hooks(EnvName())}/none-{Guid.NewGuid():N}/ping", Empty());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task RedeliverUnknownEventReturns404()
    {
        using var client = app.CreateAuthedClient();
        var env = EnvName();
        var name = await CreateHook(client, env);
        using var res = await client.PostAsync($"{Hooks(env)}/{name}/deliveries/env_updated/redeliver", Empty());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task UpdatingAnEnvironmentFiresWebhook()
    {
        using var client = app.CreateAuthedClient();
        var env = EnvName();
        // Preview webhooks resolve to the "default" project; create the env there so firing matches the scope.
        (await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = "default", Name = env })).EnsureSuccessStatusCode();
        var name = await CreateHook(client, env);

        using var patch = await client.PatchAsync($"/api/esc/environments/{Org}/default/{env}",
            new StringContent("values:\n  greeting: hello\n", Encoding.UTF8, "application/json"));
        patch.EnsureSuccessStatusCode();

        var deliveries = await client.GetFromJsonAsync<List<WebhookDelivery>>($"{Hooks(env)}/{name}/deliveries");
        Assert.Contains(deliveries!, d => d.Kind == "env_updated");
    }
}
