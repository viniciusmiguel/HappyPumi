using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for environment drafts (proposed definition changes).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscDraftTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-drafts";

    [Fact]
    public async Task DraftCreateReadUpdateOpenLifecycle()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        var created = await (await client.PatchOrPostYaml($"{EnvUrl(name)}/drafts", "values:\n  region: us-west-2\n"))
            .Content.ReadFromJsonAsync<ChangeRequestRef>();
        Assert.False(string.IsNullOrEmpty(created!.ChangeRequestId));
        var id = created.ChangeRequestId!;

        Assert.Contains("us-west-2", await client.GetStringAsync($"{EnvUrl(name)}/drafts/{id}"));

        using var patch = await client.PatchAsync($"{EnvUrl(name)}/drafts/{id}", Yaml("values:\n  region: eu-west-1\n"));
        patch.EnsureSuccessStatusCode();
        Assert.Contains("eu-west-1", await client.GetStringAsync($"{EnvUrl(name)}/drafts/{id}"));

        using var open = await client.PostAsync($"{EnvUrl(name)}/drafts/{id}/open", EmptyJson());
        open.EnsureSuccessStatusCode();
        var session = await open.Content.ReadFromJsonAsync<OpenEnvironmentResponse>();
        Assert.False(string.IsNullOrEmpty(session!.Id));
    }

    [Fact]
    public async Task ReadUnknownDraftReturns404()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{EnvUrl(name)}/drafts/bogus")).StatusCode);
    }

    [Fact]
    public async Task CreateDraftForMissingEnvironmentReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.PostAsync($"{EnvUrl($"missing-{Guid.NewGuid():N}")}/drafts", Yaml("values: {}\n"));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private static StringContent Yaml(string yaml) => new(yaml, Encoding.UTF8, "application/x-yaml");
    private static StringContent EmptyJson() => new("{}", Encoding.UTF8, "application/json");
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

file static class DraftHttpExtensions
{
    public static Task<HttpResponseMessage> PatchOrPostYaml(this HttpClient client, string url, string yaml)
        => client.PostAsync(url, new StringContent(yaml, Encoding.UTF8, "application/x-yaml"));
}
