using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for the Tier-A ESC lifecycle endpoints: update, delete, check, decrypt, open-yaml.</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscLifecycleTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-lifecycle";

    private const string SecretYaml = """
    values:
      aws:
        region: us-west-2
      db:
        password:
          fn::secret: hunter2
      env:
        r: ${aws.region}
    """;

    [Fact]
    public async Task UpdateThenReadReflectsNewDefinition()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        using var patch = await client.PatchAsync(EnvUrl(name), Yaml(SecretYaml));
        patch.EnsureSuccessStatusCode();
        var body = await patch.Content.ReadFromJsonAsync<UpdateEnvironmentResponse>();
        Assert.Empty(body!.Diagnostics!);

        var read = await client.GetStringAsync(EnvUrl(name));
        Assert.Contains("us-west-2", read);
    }

    [Fact]
    public async Task UpdateInvalidDefinitionReturnsDiagnostics()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        using var patch = await client.PatchAsync(EnvUrl(name), Yaml("values: [unterminated"));
        patch.EnsureSuccessStatusCode();
        var body = await patch.Content.ReadFromJsonAsync<UpdateEnvironmentResponse>();
        Assert.NotEmpty(body!.Diagnostics!);
        Assert.Equal("error", body.Diagnostics![0].Severity);
    }

    [Fact]
    public async Task DeleteRemovesEnvironment()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        using var del = await client.DeleteAsync(EnvUrl(name));
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        using var meta = await client.GetAsync($"{EnvUrl(name)}/metadata");
        Assert.Equal(HttpStatusCode.NotFound, meta.StatusCode);
    }

    [Fact]
    public async Task DeleteUnknownEnvironmentReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var del = await client.DeleteAsync(EnvUrl($"missing-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.NotFound, del.StatusCode);
    }

    [Fact]
    public async Task CheckMasksSecretsUnlessRequested()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        (await client.PatchAsync(EnvUrl(name), Yaml(SecretYaml))).EnsureSuccessStatusCode();

        var masked = await Check(client, name, showSecrets: false);
        var pw = Children(masked["db"])["password"];
        Assert.True(pw.Secret);
        Assert.Null(pw.Value); // masked

        var shown = await Check(client, name, showSecrets: true);
        var pwShown = Children(shown["db"])["password"];
        Assert.Equal("hunter2", pwShown.Value!.ToString());
    }

    [Fact]
    public async Task DecryptReturnsYamlDefinition()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        (await client.PatchAsync(EnvUrl(name), Yaml(SecretYaml))).EnsureSuccessStatusCode();

        using var res = await client.GetAsync($"{EnvUrl(name)}/decrypt");
        res.EnsureSuccessStatusCode();
        Assert.Contains("x-yaml", res.Content.Headers.ContentType!.ToString());
        Assert.Contains("hunter2", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OpenYamlReturnsSessionId()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.PostAsync($"/api/esc/environments/{Org}/yaml/open?duration=5m", Yaml(SecretYaml));
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<OpenEnvironmentResponse>();
        Assert.False(string.IsNullOrEmpty(body!.Id));
    }

    private static async Task<Dictionary<string, EscValue>> Check(HttpClient client, string name, bool showSecrets)
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var res = await client.PostAsync($"{EnvUrl(name)}/check?showSecrets={showSecrets.ToString().ToLowerInvariant()}", content);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<CheckEnvironmentResponse>();
        return body!.Properties!;
    }

    private static string EnvUrl(string name) => $"/api/esc/environments/{Org}/{Project}/{name}";
    private static StringContent Yaml(string yaml) => new(yaml, Encoding.UTF8, "application/x-yaml");

    // EscValue.Value round-trips through JSON as a JsonElement; re-read the nested EscValue map.
    private static Dictionary<string, EscValue> Children(EscValue v) =>
        JsonSerializer.Deserialize<Dictionary<string, EscValue>>(JsonSerializer.Serialize(v.Value))!;

    private static async Task<string> CreateEnv(HttpClient client)
    {
        var name = $"env-{Guid.NewGuid():N}";
        using var res = await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });
        res.EnsureSuccessStatusCode();
        return name;
    }
}
