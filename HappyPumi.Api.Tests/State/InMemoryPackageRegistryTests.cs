using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>Unit tests for the in-memory package registry and its publish handshake (ENDPOINTS.md 4).</summary>
public sealed class InMemoryPackageRegistryTests
{
    private static PackageCoordinates Coords() => new("private", "acme", "widgets");

    [Fact]
    public void PublishHandshakeMarksVersionPublished()
    {
        var registry = new InMemoryPackageRegistry();

        var started = registry.StartPublish(Coords(), "1.0.0", publishedAt: null);
        Assert.False(started.Published);

        Assert.True(registry.CompletePublish(Coords(), "1.0.0"));
        Assert.True(registry.Get(Coords(), "1.0.0")!.Published);
    }

    [Fact]
    public void CompletePublishOnUnknownVersionIsFalse()
    {
        var registry = new InMemoryPackageRegistry();
        Assert.False(registry.CompletePublish(Coords(), "9.9.9"));
    }

    [Fact]
    public void GetLatestReturnsMostRecentlyCreated()
    {
        var registry = new InMemoryPackageRegistry();
        registry.StartPublish(Coords(), "1.0.0", null);
        registry.StartPublish(Coords(), "2.0.0", null);

        Assert.Equal("2.0.0", registry.Get(Coords(), "latest")!.Version);
    }

    [Fact]
    public void ListLatestReturnsOnePerPackageAndFiltersByName()
    {
        var registry = new InMemoryPackageRegistry();
        registry.StartPublish(Coords(), "1.0.0", null);
        registry.StartPublish(Coords(), "2.0.0", null);
        registry.StartPublish(new PackageCoordinates("private", "acme", "gadgets"), "1.0.0", null);

        Assert.Equal(2, registry.ListLatest(null).Count);          // one per package
        Assert.Single(registry.ListLatest("widget"));              // name substring filter
    }

    [Fact]
    public void DeleteRemovesTheVersion()
    {
        var registry = new InMemoryPackageRegistry();
        registry.StartPublish(Coords(), "1.0.0", null);

        Assert.True(registry.Delete(Coords(), "1.0.0"));
        Assert.Null(registry.Get(Coords(), "1.0.0"));
        Assert.False(registry.Delete(Coords(), "1.0.0"));
    }
}
