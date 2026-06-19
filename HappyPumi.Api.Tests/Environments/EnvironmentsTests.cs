using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for the ESC environments backend: CRUD, revisions, and the YAML evaluator.</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EnvironmentsTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-test";

    private static async Task<string> NewEnv(HttpClient client)
    {
        var name = $"env-{Guid.NewGuid():N}";
        using var res = await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });
        res.EnsureSuccessStatusCode();
        return name;
    }

    [Fact]
    public async Task CreateThenListReadMetadataSettingsRevisions()
    {
        using var client = app.CreateAuthedClient();
        var name = await NewEnv(client);

        var list = await client.GetFromJsonAsync<ListEnvironmentsResponse>($"/api/esc/environments/{Org}");
        Assert.Contains(list!.Environments, e => e.Name == name && e.Project == Project);

        using var read = await client.GetAsync($"/api/esc/environments/{Org}/{Project}/{name}");
        read.EnsureSuccessStatusCode();
        Assert.Contains("x-yaml", read.Content.Headers.ContentType!.ToString());

        var meta = await client.GetFromJsonAsync<EnvironmentMetadata>($"/api/esc/environments/{Org}/{Project}/{name}/metadata");
        Assert.Equal($"{Org}/{Project}/{name}", meta!.Id);
        Assert.False(meta.OpenRequestNeeded);

        var settings = await client.GetFromJsonAsync<EnvironmentSettings>($"/api/esc/environments/{Org}/{Project}/{name}/settings");
        Assert.False(settings!.DeletionProtected);

        var revisions = await client.GetFromJsonAsync<List<EnvironmentRevision>>($"/api/esc/environments/{Org}/{Project}/{name}/versions");
        Assert.Single(revisions!); // initial revision
        Assert.Equal(1, revisions![0].Number);
    }

    [Fact]
    public async Task DuplicateCreateReturns409()
    {
        using var client = app.CreateAuthedClient();
        var name = await NewEnv(client);

        using var dup = await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });

        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task CheckYamlEvaluatesInterpolationAndSecrets()
    {
        using var client = app.CreateAuthedClient();
        const string yaml = "values:\n  aws:\n    region: us-west-2\n  env:\n    r: ${aws.region}\n  s:\n    fn::secret: hunter2\n";
        using var content = new StringContent(yaml, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-yaml");

        using var res = await client.PostAsync($"/api/esc/environments/{Org}/yaml/check", content);

        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<EnvironmentResponse>();
        var props = body!.Properties!;
        var env = (Dictionary<string, EscValue>)JsonValue(props["env"]);
        Assert.Equal("us-west-2", env["r"].Value!.ToString());
        Assert.True(props["s"].Secret);
    }

    [Fact]
    public async Task UnknownEnvironmentReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.GetAsync($"/api/esc/environments/{Org}/{Project}/missing-{Guid.NewGuid():N}/metadata");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // EscValue.Value round-trips through JSON as a JsonElement; re-read the nested map.
    private static Dictionary<string, EscValue> JsonValue(EscValue v)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(v.Value);
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, EscValue>>(json)!;
    }
}
