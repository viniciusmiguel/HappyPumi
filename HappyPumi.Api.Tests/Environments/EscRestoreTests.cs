using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for soft-delete + restore (schema-backed, AddEnvironmentSoftDelete).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscRestoreTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-restore";

    [Fact]
    public async Task DeletedEnvironmentCanBeRestored()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        (await client.PatchAsync(EnvUrl(name), Yaml("values:\n  region: us-west-2\n"))).EnsureSuccessStatusCode();

        (await client.DeleteAsync(EnvUrl(name))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{EnvUrl(name)}/metadata")).StatusCode);

        using var restore = await client.PutAsJsonAsync($"/api/esc/environments/{Org}/restore",
            new { project = Project, name });
        Assert.Equal(HttpStatusCode.NoContent, restore.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{EnvUrl(name)}/metadata")).StatusCode);
        Assert.Contains("us-west-2", await client.GetStringAsync(EnvUrl(name))); // definition preserved
    }

    [Fact]
    public async Task RestoreUnknownReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.PutAsJsonAsync($"/api/esc/environments/{Org}/restore",
            new { project = Project, name = $"missing-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CreatingOverASoftDeletedNameConflicts()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        (await client.DeleteAsync(EnvUrl(name))).EnsureSuccessStatusCode();

        using var recreate = await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });
        Assert.Equal(HttpStatusCode.Conflict, recreate.StatusCode); // name is reserved until restored
    }

    private static string EnvUrl(string name) => $"/api/esc/environments/{Org}/{Project}/{name}";
    private static StringContent Yaml(string yaml) => new(yaml, Encoding.UTF8, "application/x-yaml");

    private static async Task<string> CreateEnv(HttpClient client)
    {
        var name = $"env-{Guid.NewGuid():N}";
        using var res = await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });
        res.EnsureSuccessStatusCode();
        return name;
    }
}
