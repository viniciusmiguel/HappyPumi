using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for environment webhook CRUD.</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscWebhookTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-hooks";

    [Fact]
    public async Task WebhookCrudLifecycle()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        var created = await (await client.PostAsJsonAsync($"{EnvUrl(name)}/hooks",
            new Webhook { Name = "ci", PayloadUrl = "https://hooks.example.com/ci", Active = true, Secret = "shh" }))
            .Content.ReadFromJsonAsync<WebhookResponse>();
        Assert.Equal("ci", created!.Name);
        Assert.True(created.HasSecret);
        Assert.Null(created.Secret); // secret never echoed

        var got = await client.GetFromJsonAsync<WebhookResponse>($"{EnvUrl(name)}/hooks/ci");
        Assert.Equal("https://hooks.example.com/ci", got!.PayloadUrl);

        var list = await client.GetFromJsonAsync<System.Collections.Generic.List<WebhookResponse>>($"{EnvUrl(name)}/hooks");
        Assert.Contains(list!, w => w.Name == "ci");

        // Update: change url + deactivate (omit secret -> kept).
        (await client.PatchAsJsonAsync($"{EnvUrl(name)}/hooks/ci",
            new Webhook { Name = "ci", PayloadUrl = "https://hooks.example.com/ci2", Active = false }))
            .EnsureSuccessStatusCode();
        var updated = await client.GetFromJsonAsync<WebhookResponse>($"{EnvUrl(name)}/hooks/ci");
        Assert.Equal("https://hooks.example.com/ci2", updated!.PayloadUrl);
        Assert.False(updated.Active);
        Assert.True(updated.HasSecret); // secret retained across update

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{EnvUrl(name)}/hooks/ci")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{EnvUrl(name)}/hooks/ci")).StatusCode);
    }

    [Fact]
    public async Task DuplicateWebhookReturns409()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        var body = new Webhook { Name = "dup", PayloadUrl = "https://x" };
        (await client.PostAsJsonAsync($"{EnvUrl(name)}/hooks", body)).EnsureSuccessStatusCode();
        using var dup = await client.PostAsJsonAsync($"{EnvUrl(name)}/hooks", body);
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task CreateForMissingEnvironmentReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.PostAsJsonAsync($"{EnvUrl($"missing-{Guid.NewGuid():N}")}/hooks",
            new Webhook { Name = "ci", PayloadUrl = "https://x" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
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
