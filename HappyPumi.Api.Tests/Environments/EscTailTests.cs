using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for the Tier-A tail: by-version read/open/decrypt, HEAD, and referrers.</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscTailTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-tail";

    [Fact]
    public async Task ReadByVersionReturnsThatRevisionsYaml()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        (await client.PatchAsync(EnvUrl(name), Yaml("values:\n  region: us-west-2\n"))).EnsureSuccessStatusCode();

        Assert.DoesNotContain("us-west-2", await client.GetStringAsync($"{EnvUrl(name)}/versions/1"));
        Assert.Contains("us-west-2", await client.GetStringAsync($"{EnvUrl(name)}/versions/2"));
        Assert.Contains("us-west-2", await client.GetStringAsync($"{EnvUrl(name)}/versions/latest")); // tag resolves
    }

    [Fact]
    public async Task ReadMissingVersionReturns404()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{EnvUrl(name)}/versions/999")).StatusCode);
    }

    [Fact]
    public async Task DecryptByVersionReturnsYaml()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        (await client.PatchAsync(EnvUrl(name), Yaml("values:\n  k:\n    fn::secret: s3cret\n"))).EnsureSuccessStatusCode();

        using var res = await client.GetAsync($"{EnvUrl(name)}/versions/2/decrypt");
        res.EnsureSuccessStatusCode();
        Assert.Contains("s3cret", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OpenByVersionReturnsSession()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        (await client.PatchAsync(EnvUrl(name), Yaml("values:\n  region: us-west-2\n"))).EnsureSuccessStatusCode();

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var res = await client.PostAsync($"{EnvUrl(name)}/versions/2/open", content);
        res.EnsureSuccessStatusCode();
        var session = await res.Content.ReadFromJsonAsync<OpenEnvironmentResponse>();
        Assert.False(string.IsNullOrEmpty(session!.Id));
    }

    [Fact]
    public async Task HeadReflectsExistence()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        Assert.Equal(HttpStatusCode.OK, (await Head(client, EnvUrl(name))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Head(client, EnvUrl($"missing-{Guid.NewGuid():N}"))).StatusCode);
    }

    [Fact]
    public async Task ReferrersListEnvironmentsThatImportThis()
    {
        using var client = app.CreateAuthedClient();
        var target = await CreateEnv(client);
        var importer = await CreateEnv(client);
        (await client.PatchAsync(EnvUrl(importer), Yaml($"imports:\n  - {Project}/{target}\nvalues:\n  x: 1\n")))
            .EnsureSuccessStatusCode();

        var referrers = await client.GetFromJsonAsync<ListEnvironmentReferrersResponse>($"{EnvUrl(target)}/referrers");
        Assert.True(referrers!.Referrers.ContainsKey($"{Project}/{importer}"));
    }

    private static async Task<HttpResponseMessage> Head(HttpClient client, string url)
    {
        using var req = new HttpRequestMessage(HttpMethod.Head, url);
        return await client.SendAsync(req);
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
