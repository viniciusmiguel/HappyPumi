using System.Linq;
using HappyPumi.Api.Contracts;
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

    [Fact]
    public void AppendEventsAccumulatesAndGetEventsReturnsThemInOrder()
    {
        var store = new InMemoryUpdateStore();
        var u = store.Create(Coords(), "update", dryRun: false);

        store.AppendEvents(u.UpdateId, new[] { Event(1), Event(2) });
        store.AppendEvents(u.UpdateId, new[] { Event(3) });

        var events = store.GetEvents(u.UpdateId);
        Assert.Equal(new long[] { 1, 2, 3 }, events.Select(e => e.Sequence).ToArray());
    }

    [Fact]
    public void GetEventsForUnknownUpdateReturnsEmpty()
    {
        var store = new InMemoryUpdateStore();
        Assert.Empty(store.GetEvents("does-not-exist"));
    }

    [Fact]
    public void ListByStackReturnsOnlyThatStacksUpdates()
    {
        var store = new InMemoryUpdateStore();
        var mine = store.Create(Coords(), "preview", dryRun: true);
        store.Create(new StackCoordinates("organization", "proj", "other"), "update", dryRun: false);

        var listed = store.ListByStack(Coords());

        Assert.Single(listed);
        Assert.Equal(mine.UpdateId, listed[0].UpdateId);
    }

    private static AppEngineEvent Event(long sequence) => new() { Sequence = sequence };
}
