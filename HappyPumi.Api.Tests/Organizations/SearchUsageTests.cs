using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for Tier 7 (ENDPOINTS.md): org resource search, NL-query parse, and usage summary.
/// No resource index/metering exists yet, so results are empty and NL parse echoes the input.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class SearchUsageTests(HappyPumiApp app)
{
    [Fact]
    public async Task ResourceSearchIsEmpty()
    {
        using var client = app.CreateClient();

        var result = await client.GetFromJsonAsync<ResourceSearchResult>("/api/orgs/acme/search/resources?query=type:aws");

        Assert.NotNull(result);
        Assert.Empty(result!.Resources!);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task NaturalLanguageParseEchoesTheQuery()
    {
        using var client = app.CreateClient();

        var parsed = await client.GetFromJsonAsync<GetNaturalLanguageQueryResponse>(
            "/api/orgs/acme/search/resources/parse?query=all+buckets");

        Assert.Equal("all buckets", parsed!.Query);
    }

    [Fact]
    public async Task UsageSummaryIsEmpty()
    {
        using var client = app.CreateClient();

        var summary = await client.GetFromJsonAsync<GetResourceCountSummaryResponse>("/api/orgs/acme/resources/summary");

        Assert.NotNull(summary);
        Assert.Empty(summary!.Summary);
    }
}
