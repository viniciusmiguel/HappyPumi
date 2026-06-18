using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Registry;

/// <summary>
/// Component tests for the Tier-4 package registry (ENDPOINTS.md): the two-phase publish handshake plus
/// list/get/delete. Each test uses a unique package name since the registry is shared across the collection.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class RegistryPackagesTests(HappyPumiApp app)
{
    private const string Source = "private";
    private const string Publisher = "acme";

    private static string Base(string name) => $"/api/registry/packages/{Source}/{Publisher}/{name}";

    [Fact]
    public async Task PublishHandshakeThenGetAndList()
    {
        using var client = app.CreateAuthedClient();
        var name = $"pkg{Guid.NewGuid():N}";

        // Phase 1: start publish -> operation id + upload URLs.
        var start = await Post<StartPackagePublishResponse>(client, $"{Base(name)}/versions",
            new StartPackagePublishRequest { Version = "1.0.0" });
        Assert.False(string.IsNullOrEmpty(start.OperationId));
        Assert.False(string.IsNullOrEmpty(start.UploadUrLs.Schema));

        // Before completion the version is pending.
        var pending = await client.GetFromJsonAsync<PackageMetadata>($"{Base(name)}/versions/1.0.0");
        Assert.Equal("pending", pending!.PackageStatus);

        // Phase 2: complete -> published.
        using var complete = await client.PostAsJsonAsync($"{Base(name)}/versions/1.0.0/complete", new { });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var published = await client.GetFromJsonAsync<PackageMetadata>($"{Base(name)}/versions/latest");
        Assert.Equal("published", published!.PackageStatus);
        Assert.Equal("1.0.0", published.Version);

        var list = await client.GetFromJsonAsync<ListPackagesResponse>($"/api/registry/packages?name={name}");
        Assert.Contains(list!.Packages, p => p.Name == name);
    }

    // The web console fetches readmeURL/schemaURL directly from the browser (cross-origin, not via its API
    // proxy), so GetPackageVersion must advertise absolute URLs rooted at the request host — a root-relative
    // path would resolve against the console origin and 404. Regression for the Private Components README.
    [Fact]
    public async Task GetVersionReturnsAbsoluteArtifactUrls()
    {
        using var client = app.CreateAuthedClient();
        var name = $"pkg{Guid.NewGuid():N}";
        await Post<StartPackagePublishResponse>(client, $"{Base(name)}/versions",
            new StartPackagePublishRequest { Version = "1.0.0" });

        var meta = await client.GetFromJsonAsync<PackageMetadata>($"{Base(name)}/versions/1.0.0");

        Assert.StartsWith("http", meta!.ReadmeUrl);
        Assert.EndsWith($"{Base(name)}/versions/1.0.0/readme", meta.ReadmeUrl);
        Assert.StartsWith("http", meta.SchemaUrl);
        Assert.EndsWith($"{Base(name)}/versions/1.0.0/schema", meta.SchemaUrl);
    }

    // ── Console component surfaces (versions list, readme, nav) ─────────────────
    [Fact]
    public async Task ListVersionsReturnsAllNewestFirstWithLatestFlag()
    {
        using var client = app.CreateAuthedClient();
        var name = $"pkg{Guid.NewGuid():N}";
        await Publish(client, name, "1.0.0");
        await Publish(client, name, "2.0.0");

        var list = await client.GetFromJsonAsync<ListPackagesResponse>($"{Base(name)}/versions");

        Assert.Equal(2, list!.Packages.Count);
        Assert.Equal("2.0.0", list.Packages[0].Version); // newest first
        Assert.True(list.Packages[0].IsLatest);
        Assert.False(list.Packages[1].IsLatest);
    }

    [Fact]
    public async Task ReadmeReturnsMarkdownText()
    {
        using var client = app.CreateAuthedClient();
        var name = $"pkg{Guid.NewGuid():N}";
        await Publish(client, name, "1.0.0");

        using var res = await client.GetAsync($"{Base(name)}/versions/1.0.0/readme");

        res.EnsureSuccessStatusCode();
        Assert.Contains("text/markdown", res.Content.Headers.ContentType!.ToString());
        var body = await res.Content.ReadAsStringAsync();
        Assert.StartsWith("#", body.TrimStart());
    }

    [Fact]
    public async Task NavReturnsModulesEnvelope()
    {
        using var client = app.CreateAuthedClient();
        var name = $"pkg{Guid.NewGuid():N}";
        await Publish(client, name, "1.0.0");

        var nav = await client.GetFromJsonAsync<GetPackageNavResponse>($"{Base(name)}/versions/latest/nav");

        Assert.Equal(name, nav!.Name);
        Assert.NotNull(nav.Modules);
    }

    [Fact]
    public async Task ReadmeAndNavReturn404ForUnknownPackage()
    {
        using var client = app.CreateAuthedClient();
        var name = $"pkg{Guid.NewGuid():N}";

        using var readme = await client.GetAsync($"{Base(name)}/versions/1.0.0/readme");
        using var nav = await client.GetAsync($"{Base(name)}/versions/1.0.0/nav");

        Assert.Equal(HttpStatusCode.NotFound, readme.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, nav.StatusCode);
    }

    private static async Task Publish(HttpClient client, string name, string version)
    {
        await Post<StartPackagePublishResponse>(client, $"{Base(name)}/versions",
            new StartPackagePublishRequest { Version = version });
        using var complete = await client.PostAsJsonAsync($"{Base(name)}/versions/{version}/complete", new { });
        complete.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetUnknownVersionReturns404()
    {
        using var client = app.CreateAuthedClient();

        using var response = await client.GetAsync($"{Base($"pkg{Guid.NewGuid():N}")}/versions/1.0.0");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublishWithoutVersionReturns400()
    {
        using var client = app.CreateAuthedClient();

        using var response = await client.PostAsJsonAsync($"{Base($"pkg{Guid.NewGuid():N}")}/versions",
            new StartPackagePublishRequest { Version = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRemovesTheVersion()
    {
        using var client = app.CreateAuthedClient();
        var name = $"pkg{Guid.NewGuid():N}";
        await Post<StartPackagePublishResponse>(client, $"{Base(name)}/versions",
            new StartPackagePublishRequest { Version = "1.0.0" });

        using var deleted = await client.DeleteAsync($"{Base(name)}/versions/1.0.0");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var missing = await client.GetAsync($"{Base(name)}/versions/1.0.0");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
