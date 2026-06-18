using System.Text.Json;

namespace HappyPumi.Api.Tests.Miscellaneous;

/// <summary>
/// Component tests for GET /api/openapi/pulumi-spec.json (Tier 0). Serves the embedded OpenAPI
/// contract verbatim; the body must be the JSON document the API is built from.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class FetchRestSpecificationTests(HappyPumiApp app)
{
    [Fact]
    public async Task Returns200WithJsonContentType()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/api/openapi/pulumi-spec.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task BodyIsTheParseableOpenApiDocument()
    {
        using var client = app.CreateClient();

        await using var stream = await client.GetStreamAsync("/api/openapi/pulumi-spec.json");
        using var doc = await JsonDocument.ParseAsync(stream);

        // The spec is an OpenAPI v3 document, so it must expose the "components" + "paths" roots.
        Assert.True(doc.RootElement.TryGetProperty("components", out _));
        Assert.True(doc.RootElement.TryGetProperty("paths", out _));
    }
}
