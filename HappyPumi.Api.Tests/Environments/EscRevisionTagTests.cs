using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for revision-tag CRUD (named pointers to a revision number).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscRevisionTagTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-revtags";

    // Creates an environment and edits it twice, leaving revisions 1, 2 and 3.
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
    public async Task RevisionTagCrudLifecycle()
    {
        using var client = app.CreateAuthedClient();
        var name = await EnvWithThreeRevisions(client);

        var created = await (await client.PostAsJsonAsync($"{EnvUrl(name)}/versions/tags",
            new CreateEnvironmentRevisionTagRequest { Name = "stable", Revision = 2 }))
            .Content.ReadFromJsonAsync<EnvironmentRevisionTag>();
        Assert.Equal(2, created!.Revision);

        var read = await client.GetFromJsonAsync<EnvironmentRevisionTag>($"{EnvUrl(name)}/versions/tags/stable");
        Assert.Equal(2, read!.Revision);

        // Move the tag to revision 3.
        (await client.PatchAsJsonAsync($"{EnvUrl(name)}/versions/tags/stable",
            new UpdateEnvironmentRevisionTagRequest { Revision = 3 })).EnsureSuccessStatusCode();
        var moved = await client.GetFromJsonAsync<EnvironmentRevisionTag>($"{EnvUrl(name)}/versions/tags/stable");
        Assert.Equal(3, moved!.Revision);

        // Delete it.
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{EnvUrl(name)}/versions/tags/stable")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{EnvUrl(name)}/versions/tags/stable")).StatusCode);
    }

    [Fact]
    public async Task DuplicateRevisionTagReturns409()
    {
        using var client = app.CreateAuthedClient();
        var name = await EnvWithThreeRevisions(client);
        var body = new CreateEnvironmentRevisionTagRequest { Name = "prod", Revision = 1 };
        (await client.PostAsJsonAsync($"{EnvUrl(name)}/versions/tags", body)).EnsureSuccessStatusCode();
        using var dup = await client.PostAsJsonAsync($"{EnvUrl(name)}/versions/tags", body);
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task CreateForMissingRevisionReturns404()
    {
        using var client = app.CreateAuthedClient();
        var name = await EnvWithThreeRevisions(client);
        using var res = await client.PostAsJsonAsync($"{EnvUrl(name)}/versions/tags",
            new CreateEnvironmentRevisionTagRequest { Name = "ghost", Revision = 999 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task LatestRevisionTagCannotBeDeleted()
    {
        using var client = app.CreateAuthedClient();
        var name = await EnvWithThreeRevisions(client);
        using var res = await client.DeleteAsync($"{EnvUrl(name)}/versions/tags/latest");
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task ListRevisionTagsForVersionIncludesTheTag()
    {
        using var client = app.CreateAuthedClient();
        var name = await EnvWithThreeRevisions(client);
        (await client.PostAsJsonAsync($"{EnvUrl(name)}/versions/tags",
            new CreateEnvironmentRevisionTagRequest { Name = "stable", Revision = 2 })).EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<ListEnvironmentRevisionTagsResponse>($"{EnvUrl(name)}/versions/2/tags");
        Assert.Contains(list!.Tags, t => t.Name == "stable" && t.Revision == 2);
    }

    private static string EnvUrl(string name) => $"/api/esc/environments/{Org}/{Project}/{name}";
    private static StringContent Yaml(string yaml) => new(yaml, Encoding.UTF8, "application/x-yaml");
}
