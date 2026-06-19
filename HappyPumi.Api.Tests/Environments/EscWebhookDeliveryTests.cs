using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for webhook delivery: ping, deliveries, redeliver (real sender, unreachable URL).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscWebhookDeliveryTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-hook-delivery";

    [Fact]
    public async Task PingRecordsDeliveryAndRedeliverResends()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        // Unreachable URL: the sender records the failed attempt (responseCode 0) rather than throwing.
        (await client.PostAsJsonAsync($"{EnvUrl(name)}/hooks",
            new Webhook { Name = "ci", PayloadUrl = "http://127.0.0.1:1/hook" })).EnsureSuccessStatusCode();

        var ping = await (await client.PostAsync($"{EnvUrl(name)}/hooks/ci/ping", Empty()))
            .Content.ReadFromJsonAsync<WebhookDelivery>();
        Assert.Equal("ping", ping!.Kind);
        Assert.Equal(0, ping.ResponseCode); // connection refused, captured not thrown

        var deliveries = await client.GetFromJsonAsync<List<WebhookDelivery>>($"{EnvUrl(name)}/hooks/ci/deliveries");
        Assert.Contains(deliveries!, d => d.Id == ping.Id);

        using var redeliver = await client.PostAsync($"{EnvUrl(name)}/hooks/ci/deliveries/{ping.Id}/redeliver", Empty());
        redeliver.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PingMissingWebhookReturns404()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        using var res = await client.PostAsync($"{EnvUrl(name)}/hooks/nope/ping", Empty());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task RedeliverUnknownEventReturns404()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        (await client.PostAsJsonAsync($"{EnvUrl(name)}/hooks",
            new Webhook { Name = "ci", PayloadUrl = "http://127.0.0.1:1/hook" })).EnsureSuccessStatusCode();
        using var res = await client.PostAsync($"{EnvUrl(name)}/hooks/ci/deliveries/bogus/redeliver", Empty());
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
