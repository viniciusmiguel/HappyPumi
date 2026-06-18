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
        using var client = app.CreateClient();
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

    [Fact]
    public async Task GetUnknownVersionReturns404()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync($"{Base($"pkg{Guid.NewGuid():N}")}/versions/1.0.0");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublishWithoutVersionReturns400()
    {
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync($"{Base($"pkg{Guid.NewGuid():N}")}/versions",
            new StartPackagePublishRequest { Version = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRemovesTheVersion()
    {
        using var client = app.CreateClient();
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
