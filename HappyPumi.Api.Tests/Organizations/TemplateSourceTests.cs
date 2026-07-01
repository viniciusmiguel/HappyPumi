using System;
using System.Linq;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the org template-source CRUD endpoints (templates PR1) against the real Postgres-backed
/// <c>ITemplateSourceStore</c>: POST create (valid https URL → IsValid; a malformed URL → invalid + error),
/// GET list reflects it, PATCH update is reflected, DELETE returns 204 then the source is gone. Unique org
/// per test for independence.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class TemplateSourceTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    private static UpsertOrgTemplateSourceRequest Sample(string name, string sourceUrl, string? destination = null) => new()
    {
        Name = name, SourceUrl = sourceUrl, DestinationUrl = destination,
    };

    [Fact]
    public async Task CreateReadUpdateDeleteLifecycle()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var basePath = $"/api/orgs/{org}/templates/sources";

        var created = await Post(client, basePath,
            Sample("team-templates", "https://example.com/templates.git", "https://dest.example.com"));
        Assert.False(string.IsNullOrWhiteSpace(created.Id));
        Assert.True(created.IsValid);
        Assert.Null(created.Error);
        Assert.Equal("https://dest.example.com", created.Destination!.Url);

        var list = await client.GetFromJsonAsync<GetOrgTemplateSourcesResponse>(basePath);
        Assert.Contains(list!.Sources, s => s.Id == created.Id && s.Name == "team-templates");

        using var patch = await client.PatchAsJsonAsync($"{basePath}/{created.Id}",
            Sample("renamed", "https://example.com/other.git"));
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var updated = (await patch.Content.ReadFromJsonAsync<TemplateSource>())!;
        Assert.Equal("renamed", updated.Name);
        Assert.Equal("https://example.com/other.git", updated.SourceUrl);

        using var deleted = await client.DeleteAsync($"{basePath}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await client.GetFromJsonAsync<GetOrgTemplateSourcesResponse>(basePath);
        Assert.DoesNotContain(afterDelete!.Sources, s => s.Id == created.Id);
    }

    [Fact]
    public async Task MalformedSourceUrlIsRecordedInvalid()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        var created = await Post(client, $"/api/orgs/{org}/templates/sources",
            Sample("bad", "not-a-url"));

        Assert.False(created.IsValid);
        Assert.Contains("not-a-url", created.Error);
    }

    [Fact]
    public async Task BlankNameOrSourceUrlIsRejected()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var basePath = $"/api/orgs/{org}/templates/sources";

        using var noName = await client.PostAsJsonAsync(basePath, Sample("", "https://example.com/t.git"));
        Assert.Equal(HttpStatusCode.BadRequest, noName.StatusCode);

        using var noUrl = await client.PostAsJsonAsync(basePath, Sample("x", ""));
        Assert.Equal(HttpStatusCode.BadRequest, noUrl.StatusCode);
    }

    [Fact]
    public async Task UpdateAndDeleteMissingSourceAre404()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var basePath = $"/api/orgs/{org}/templates/sources";

        using var patch = await client.PatchAsJsonAsync($"{basePath}/ghost",
            Sample("x", "https://example.com/t.git"));
        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);

        using var delete = await client.DeleteAsync($"{basePath}/ghost");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    private static async Task<TemplateSource> Post(HttpClient client, string url, UpsertOrgTemplateSourceRequest body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TemplateSource>())!;
    }
}
