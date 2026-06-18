using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Registry;

/// <summary>
/// Component tests for the Tier-4 template registry, org templates, and the AI template endpoint
/// (ENDPOINTS.md). Each test uses a unique template name since the registry is shared.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class RegistryTemplatesTests(HappyPumiApp app)
{
    private const string Source = "private";
    private const string Publisher = "acme";

    private static string Base(string name) => $"/api/registry/templates/{Source}/{Publisher}/{name}";

    [Fact]
    public async Task PublishListVersionsAndGet()
    {
        using var client = app.CreateClient();
        var name = $"tmpl{Guid.NewGuid():N}";

        var start = await Post<StartTemplatePublishResponse>(client, $"{Base(name)}/versions",
            new StartTemplatePublishRequest { Version = "1.0.0" });
        Assert.False(string.IsNullOrEmpty(start.UploadUrLs.Archive));

        using var complete = await client.PostAsJsonAsync($"{Base(name)}/versions/1.0.0/complete", new { });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var versions = await client.GetFromJsonAsync<ListTemplateVersionsResponse>($"{Base(name)}/versions");
        Assert.Contains(versions!.Templates, v => v.Version == "1.0.0");

        var tmpl = await client.GetFromJsonAsync<GetTemplateResponse>($"{Base(name)}/versions/latest");
        Assert.Equal(name, tmpl!.Name);

        var list = await client.GetFromJsonAsync<ListTemplatesResponse>($"/api/registry/templates?name={name}");
        Assert.Contains(list!.Templates, t => t.Name == name);
    }

    [Fact]
    public async Task GetUnknownTemplateVersionReturns404()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync($"{Base($"tmpl{Guid.NewGuid():N}")}/versions/1.0.0");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRemovesTheTemplateVersion()
    {
        using var client = app.CreateClient();
        var name = $"tmpl{Guid.NewGuid():N}";
        await Post<StartTemplatePublishResponse>(client, $"{Base(name)}/versions",
            new StartTemplatePublishRequest { Version = "1.0.0" });

        using var deleted = await client.DeleteAsync($"{Base(name)}/versions/1.0.0");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var missing = await client.GetAsync($"{Base(name)}/versions/1.0.0");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task OrgTemplatesReportsNone()
    {
        using var client = app.CreateClient();

        var response = await client.GetFromJsonAsync<GetOrgTemplatesResponse>($"/api/orgs/acme/templates");

        Assert.NotNull(response);
        Assert.False(response!.OrgHasTemplates);
        Assert.Empty(response.Templates);
    }

    [Fact]
    public async Task AiTemplateIsNotImplemented()
    {
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/ai/template", new { language = "go" });

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
