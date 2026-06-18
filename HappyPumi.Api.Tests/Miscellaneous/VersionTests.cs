using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Miscellaneous;

/// <summary>
/// Component tests for GET /api/cli/version (Tier 0). The CLI's update-check reads these fields; they
/// must be present and parseable as versions.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class VersionTests(HappyPumiApp app)
{
    [Fact]
    public async Task Returns200()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/api/cli/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdvertisesAllThreeVersionFields()
    {
        using var client = app.CreateClient();

        var version = await client.GetFromJsonAsync<AppCliVersionResponse>("/api/cli/version");

        Assert.NotNull(version);
        Assert.False(string.IsNullOrWhiteSpace(version!.LatestVersion));
        Assert.False(string.IsNullOrWhiteSpace(version.LatestDevVersion));
        Assert.False(string.IsNullOrWhiteSpace(version.OldestWithoutWarning));
    }
}
