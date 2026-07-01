using System.Text.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the resource-search and usage-summary endpoints (PR4) against the real Postgres host.
/// HappyPumi has no resource-search index or usage-metering data source, so these endpoints return
/// deterministic empty result sets / zero aggregations (design doc 2026-07-01). The point of these tests is
/// to guard that none of the eight endpoints 500s and that their contracts serialize: each returns 200 with
/// the expected empty/zero shape, the CSV export carries a header row, the HEAD probe is a 200, packages-usage
/// echoes the queried package name with no stacks, and the two summaries return the count-summary shape.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class ResourceSearchTests(HappyPumiApp app)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task ColumnFilterSetIsEmptyDictionary()
    {
        using var client = app.CreateClient();
        var set = await GetJson<Dictionary<string, Aggregation>>(client, $"/api/orgs/{NewOrg()}/search/column-set");
        Assert.Empty(set);
    }

    [Fact]
    public async Task DashboardAggregationsAreEmptyWithZeroTotal()
    {
        using var client = app.CreateClient();
        await AssertEmptySearch(client, $"/api/orgs/{NewOrg()}/search/resources/dashboard");
    }

    [Fact]
    public async Task ResourceSearchV2IsEmptyWithZeroTotal()
    {
        using var client = app.CreateClient();
        await AssertEmptySearch(client, $"/api/orgs/{NewOrg()}/search/resourcesv2");
    }

    [Fact]
    public async Task ExportReturnsCsvHeaderOnly()
    {
        using var client = app.CreateClient();
        using var response = await client.GetAsync($"/api/orgs/{NewOrg()}/search/resources/export");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("urn,type,project,stack", body);
        Assert.DoesNotContain("urn:pulumi", body); // header only — no data rows
    }

    [Fact]
    public async Task SearchClusterAvailableHeadReturns200()
    {
        using var client = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Head, $"/api/orgs/{NewOrg()}/search");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PackageUsageEchoesNameWithNoStacks()
    {
        using var client = app.CreateClient();
        var usage = await GetJson<PackageUsageResponse>(client, $"/api/orgs/{NewOrg()}/packages/usage?name=aws");
        Assert.Equal("aws", usage.PackageName);
        Assert.Empty(usage.Stacks!);
        Assert.Equal(0, usage.TotalStacks);
    }

    [Fact]
    public async Task EnvironmentSecretsSummaryReturnsCountSummaryShape()
    {
        using var client = app.CreateClient();
        var summary = await GetJson<GetResourceCountSummaryResponse>(client, $"/api/orgs/{NewOrg()}/secrets/summary");
        Assert.NotNull(summary.Summary);
        var point = Assert.Single(summary.Summary);
        Assert.Equal(0, point.Resources); // a fresh org has no ESC environments
    }

    [Fact]
    public async Task DiscoveredResourceHoursSummaryIsEmpty()
    {
        using var client = app.CreateClient();
        var summary = await GetJson<GetResourceCountSummaryResponse>(client,
            $"/api/orgs/{NewOrg()}/discovered-resources/summary");
        Assert.NotNull(summary.Summary);
        Assert.Empty(summary.Summary);
    }

    private static async Task AssertEmptySearch(HttpClient client, string url)
    {
        var result = await GetJson<ResourceSearchResult>(client, url);
        Assert.Equal(0, result.Total);
        Assert.Empty(result.Aggregations!);
        Assert.Empty(result.Resources!);
    }

    private static async Task<T> GetJson<T>(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), Json)!;
    }
}
