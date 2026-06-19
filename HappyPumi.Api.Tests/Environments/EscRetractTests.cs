using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for retracting an environment revision (schema-backed, AddRevisionRetraction).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscRetractTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-retract";

    private static async Task<string> EnvWithThreeRevisions(HttpClient client)
    {
        var name = $"env-{Guid.NewGuid():N}";
        (await client.PostAsJsonAsync($"/api/esc/environments/{Org}", new CreateEnvironmentRequest { Project = Project, Name = name }))
            .EnsureSuccessStatusCode();
        (await client.PatchAsync(EnvUrl(name), Yaml("values:\n  v: 2\n"))).EnsureSuccessStatusCode();
        (await client.PatchAsync(EnvUrl(name), Yaml("values:\n  v: 3\n"))).EnsureSuccessStatusCode();
        return name;
    }

    [Fact]
    public async Task RetractMarksRevisionInHistory()
    {
        using var client = app.CreateAuthedClient();
        var name = await EnvWithThreeRevisions(client);

        using var res = await client.PostAsJsonAsync($"{EnvUrl(name)}/versions/2/retract",
            new RetractEnvironmentRevisionRequest { Reason = "leaked secret", Replacement = 3 });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var revisions = await client.GetFromJsonAsync<List<EnvironmentRevision>>($"{EnvUrl(name)}/versions");
        var rev2 = revisions!.Single(r => r.Number == 2);
        Assert.NotNull(rev2.Retracted);
        Assert.Equal("leaked secret", rev2.Retracted!.Reason);
        Assert.Equal(3, rev2.Retracted.Replacement);

        var rev3 = revisions.Single(r => r.Number == 3);
        Assert.Null(rev3.Retracted); // unaffected
    }

    [Fact]
    public async Task RetractMissingRevisionReturns404()
    {
        using var client = app.CreateAuthedClient();
        var name = await EnvWithThreeRevisions(client);
        using var res = await client.PostAsJsonAsync($"{EnvUrl(name)}/versions/999/retract",
            new RetractEnvironmentRevisionRequest { Reason = "n/a" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task RetractByTagNameResolvesTheRevision()
    {
        using var client = app.CreateAuthedClient();
        var name = await EnvWithThreeRevisions(client);
        (await client.PostAsJsonAsync($"{EnvUrl(name)}/versions/tags",
            new CreateEnvironmentRevisionTagRequest { Name = "stable", Revision = 1 })).EnsureSuccessStatusCode();

        using var res = await client.PostAsJsonAsync($"{EnvUrl(name)}/versions/stable/retract",
            new RetractEnvironmentRevisionRequest { Reason = "rollback" });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var revisions = await client.GetFromJsonAsync<List<EnvironmentRevision>>($"{EnvUrl(name)}/versions");
        Assert.NotNull(revisions!.Single(r => r.Number == 1).Retracted);
    }

    private static string EnvUrl(string name) => $"/api/esc/environments/{Org}/{Project}/{name}";
    private static StringContent Yaml(string yaml) => new(yaml, Encoding.UTF8, "application/x-yaml");
}
