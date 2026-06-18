using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>Unit tests for the in-memory template registry (ENDPOINTS.md 4).</summary>
public sealed class InMemoryTemplateRegistryTests
{
    private static TemplateCoordinates Coords() => new("private", "acme", "web");

    [Fact]
    public void PublishHandshakeAndListVersions()
    {
        var registry = new InMemoryTemplateRegistry();

        Assert.False(registry.StartPublish(Coords(), "1.0.0").Published);
        Assert.True(registry.CompletePublish(Coords(), "1.0.0"));
        Assert.True(registry.Get(Coords(), "1.0.0")!.Published);
        Assert.Single(registry.ListVersions(Coords()));
    }

    [Fact]
    public void GetLatestAndListLatest()
    {
        var registry = new InMemoryTemplateRegistry();
        registry.StartPublish(Coords(), "1.0.0");
        registry.StartPublish(Coords(), "2.0.0");

        Assert.Equal("2.0.0", registry.Get(Coords(), "latest")!.Version);
        Assert.Single(registry.ListLatest(null));
        Assert.Empty(registry.ListLatest("nomatch"));
    }

    [Fact]
    public void DeleteRemovesTheVersion()
    {
        var registry = new InMemoryTemplateRegistry();
        registry.StartPublish(Coords(), "1.0.0");

        Assert.True(registry.Delete(Coords(), "1.0.0"));
        Assert.False(registry.Delete(Coords(), "1.0.0"));
    }
}
