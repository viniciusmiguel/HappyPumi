using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for the Tier-A batch-2 management endpoints: settings, clone, ownership, tags.</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscManagementTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-mgmt";

    [Fact]
    public async Task DeletionProtectionBlocksDelete()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        (await client.PatchAsJsonAsync($"{EnvUrl(name)}/settings", new EnvironmentSettings { DeletionProtected = true }))
            .EnsureSuccessStatusCode();
        using var blocked = await client.DeleteAsync(EnvUrl(name));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        (await client.PatchAsJsonAsync($"{EnvUrl(name)}/settings", new EnvironmentSettings { DeletionProtected = false }))
            .EnsureSuccessStatusCode();
        using var allowed = await client.DeleteAsync(EnvUrl(name));
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
    }

    [Fact]
    public async Task CloneCopiesDefinitionAndTags()
    {
        using var client = app.CreateAuthedClient();
        var src = await CreateEnv(client);
        (await client.PatchAsync(EnvUrl(src), Yaml("values:\n  region: us-west-2\n"))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"{EnvUrl(src)}/tags", new CreateEnvironmentTagRequest { Name = "team", Value = "platform" }))
            .EnsureSuccessStatusCode();

        var dest = $"clone-{Guid.NewGuid():N}";
        using var clone = await client.PostAsJsonAsync($"{EnvUrl(src)}/clone",
            new CloneEnvironmentRequest { Name = dest, Project = Project, PreserveEnvironmentTags = true });
        Assert.Equal(HttpStatusCode.Created, clone.StatusCode);

        Assert.Contains("us-west-2", await client.GetStringAsync(EnvUrl(dest)));
        var tag = await client.GetFromJsonAsync<EnvironmentTag>($"{EnvUrl(dest)}/tags/team");
        Assert.Equal("platform", tag!.Value);
    }

    [Fact]
    public async Task CloneIntoExistingNameReturns409()
    {
        using var client = app.CreateAuthedClient();
        var src = await CreateEnv(client);
        var existing = await CreateEnv(client);

        using var clone = await client.PostAsJsonAsync($"{EnvUrl(src)}/clone",
            new CloneEnvironmentRequest { Name = existing, Project = Project });
        Assert.Equal(HttpStatusCode.Conflict, clone.StatusCode);
    }

    [Fact]
    public async Task ReassignOwnershipReturnsPreviousOwner()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        using var res = await client.PostAsJsonAsync($"{EnvUrl(name)}/ownership",
            new UserInfo { GithubLogin = "alice", Name = "Alice" });
        res.EnsureSuccessStatusCode();
        var previous = await res.Content.ReadFromJsonAsync<UserInfo>();
        Assert.Equal("happypumi", previous!.GithubLogin); // the creator was the previous owner
    }

    [Fact]
    public async Task TagCrudLifecycle()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        (await client.PostAsJsonAsync($"{EnvUrl(name)}/tags", new CreateEnvironmentTagRequest { Name = "team", Value = "platform" }))
            .EnsureSuccessStatusCode();

        var created = await client.GetFromJsonAsync<EnvironmentTag>($"{EnvUrl(name)}/tags/team");
        Assert.Equal("platform", created!.Value);

        // Revalue
        (await client.PatchAsJsonAsync($"{EnvUrl(name)}/tags/team", new UpdateEnvironmentTagRequest
        {
            CurrentTag = new UpdateEnvironmentTagRequestCurrentTag { Value = "platform" },
            NewTag = new UpdateEnvironmentTagRequestNewTag { Value = "infra" },
        })).EnsureSuccessStatusCode();
        var revalued = await client.GetFromJsonAsync<EnvironmentTag>($"{EnvUrl(name)}/tags/team");
        Assert.Equal("infra", revalued!.Value);

        // Rename
        (await client.PatchAsJsonAsync($"{EnvUrl(name)}/tags/team", new UpdateEnvironmentTagRequest
        {
            CurrentTag = new UpdateEnvironmentTagRequestCurrentTag { Value = "infra" },
            NewTag = new UpdateEnvironmentTagRequestNewTag { Name = "squad" },
        })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{EnvUrl(name)}/tags/team")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{EnvUrl(name)}/tags/squad")).StatusCode);

        // Delete
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{EnvUrl(name)}/tags/squad")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{EnvUrl(name)}/tags/squad")).StatusCode);
    }

    [Fact]
    public async Task DuplicateTagReturns409()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        var body = new CreateEnvironmentTagRequest { Name = "env", Value = "prod" };
        (await client.PostAsJsonAsync($"{EnvUrl(name)}/tags", body)).EnsureSuccessStatusCode();
        using var dup = await client.PostAsJsonAsync($"{EnvUrl(name)}/tags", body);
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }

    [Fact]
    public async Task ListAllTagsAggregatesAcrossOrg()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        var key = $"region-{Guid.NewGuid():N}";
        (await client.PostAsJsonAsync($"{EnvUrl(name)}/tags", new CreateEnvironmentTagRequest { Name = key, Value = "us-east-1" }))
            .EnsureSuccessStatusCode();

        var all = await client.GetFromJsonAsync<Dictionary<string, List<string>>>($"/api/esc/environments/{Org}/tags");
        Assert.Contains("us-east-1", all![key]);
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
