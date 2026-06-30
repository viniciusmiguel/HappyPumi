using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for the in-memory stack-annotation store (the default <see cref="IStackAnnotationStore"/>,
/// ADR-0005). Each test uses a fresh instance, so they are fully isolated.
/// </summary>
public sealed class InMemoryStackAnnotationStoreTests
{
    private static StackCoordinates Coords(string stack = "dev") => new("happypumi", "webapp", stack);

    [Fact]
    public void GetIsNullWhenNoAnnotationSet()
        => Assert.Null(new InMemoryStackAnnotationStore().Get(Coords(), "compliance"));

    [Fact]
    public void SetThenGetReturnsThePayload()
    {
        var store = new InMemoryStackAnnotationStore();
        var payload = new { owner = "platform" };

        store.Set(Coords(), "compliance", payload);

        Assert.Same(payload, store.Get(Coords(), "compliance"));
    }

    [Fact]
    public void SetOverwritesThePreviousPayloadForTheSameKind()
    {
        var store = new InMemoryStackAnnotationStore();
        store.Set(Coords(), "compliance", new { v = 1 });
        var second = new { v = 2 };

        store.Set(Coords(), "compliance", second);

        Assert.Same(second, store.Get(Coords(), "compliance"));
    }

    [Fact]
    public void AnnotationsAreScopedByKindAndStack()
    {
        var store = new InMemoryStackAnnotationStore();
        store.Set(Coords(), "compliance", new { a = 1 });

        Assert.Null(store.Get(Coords(), "metadata"));
        Assert.Null(store.Get(Coords("prod"), "compliance"));
    }
}
