using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for the in-memory update store (the default <see cref="IUpdateStore"/>, ADR-0005).
/// Focused on FindByVersion, which recovers the resource checkpoint for a historical stack version.
/// </summary>
public sealed class InMemoryUpdateStoreTests
{
    private static StackCoordinates Coords() => new("organization", "proj", "stack");

    [Fact]
    public void FindByVersionReturnsSucceededUpdateAtThatVersion()
    {
        var store = new InMemoryUpdateStore();
        var u = store.Create(Coords(), "update", dryRun: false);
        u.Version = 3;
        u.Status = "succeeded";
        store.Save(u);

        var found = store.FindByVersion(Coords(), 3);

        Assert.NotNull(found);
        Assert.Equal(u.UpdateId, found!.UpdateId);
        Assert.Null(store.FindByVersion(Coords(), 99));
    }

    [Fact]
    public void FindByVersionIgnoresDryRuns()
    {
        var store = new InMemoryUpdateStore();
        var u = store.Create(Coords(), "preview", dryRun: true);
        u.Version = 1;
        u.Status = "succeeded";
        store.Save(u);

        Assert.Null(store.FindByVersion(Coords(), 1));
    }
}
