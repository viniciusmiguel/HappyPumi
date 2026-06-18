using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for the in-memory stack store (the default <see cref="IStackStore"/>, ADR-0005). Each
/// test uses a fresh instance, so they are fully isolated from one another.
/// </summary>
public sealed class InMemoryStackStoreTests
{
    private static StackCoordinates Coords(string stack = "dev") => new("happypumi", "webapp", stack);

    private static StoredStack NewStack(StackCoordinates coordinates) => new() { Coordinates = coordinates };

    [Fact]
    public void TryCreateAddsANewStack()
    {
        var store = new InMemoryStackStore();

        Assert.True(store.TryCreate(NewStack(Coords())));
        Assert.NotNull(store.Find(Coords()));
    }

    [Fact]
    public void TryCreateIsRejectedForADuplicate()
    {
        var store = new InMemoryStackStore();
        store.TryCreate(NewStack(Coords()));

        Assert.False(store.TryCreate(NewStack(Coords())));
    }

    [Fact]
    public void ProjectExistsOnlyOnceAStackIsCreated()
    {
        var store = new InMemoryStackStore();
        Assert.False(store.ProjectExists("happypumi", "webapp"));

        store.TryCreate(NewStack(Coords()));

        Assert.True(store.ProjectExists("happypumi", "webapp"));
    }

    [Fact]
    public void DeleteRemovesTheStackAndReportsMissing()
    {
        var store = new InMemoryStackStore();
        store.TryCreate(NewStack(Coords()));

        Assert.True(store.Delete(Coords()));
        Assert.Null(store.Find(Coords()));
        Assert.False(store.Delete(Coords()));
    }

    [Fact]
    public void SetConfigStoresConfigOnAnExistingStack()
    {
        var store = new InMemoryStackStore();
        store.TryCreate(NewStack(Coords()));
        var config = new AppStackConfig { SecretsProvider = "passphrase" };

        var updated = store.SetConfig(Coords(), config);

        Assert.Equal("passphrase", updated?.Config?.SecretsProvider);
    }

    [Fact]
    public void SetConfigReturnsNullWhenStackIsMissing()
    {
        var store = new InMemoryStackStore();

        Assert.Null(store.SetConfig(Coords(), new AppStackConfig()));
    }

    [Fact]
    public void ClearConfigRemovesConfigButKeepsTheStack()
    {
        var store = new InMemoryStackStore();
        store.TryCreate(NewStack(Coords()));
        store.SetConfig(Coords(), new AppStackConfig { SecretsProvider = "passphrase" });

        Assert.True(store.ClearConfig(Coords()));
        Assert.Null(store.Find(Coords())?.Config);
    }

    [Fact]
    public void RecordHistoryAppendsToTheStack()
    {
        var store = new InMemoryStackStore();
        store.TryCreate(NewStack(Coords()));

        store.RecordHistory(Coords(), new StoredHistoryEntry
        {
            UpdateId = "u1",
            Info = new AppUpdateInfo { Kind = "update", Result = "succeeded", Version = 1 },
        });

        Assert.Single(store.Find(Coords())!.History);
    }

    [Fact]
    public void ReplaceTagsIsWholesale()
    {
        var store = new InMemoryStackStore();
        store.TryCreate(NewStack(Coords()));
        store.SetTag(Coords(), "env", "dev");

        store.ReplaceTags(Coords(), new Dictionary<string, string> { ["team"] = "platform" });

        var tags = store.Find(Coords())!.Tags;
        Assert.False(tags.ContainsKey("env"));
        Assert.Equal("platform", tags["team"]);
    }

    [Fact]
    public void RenameMovesAndReportsCollision()
    {
        var store = new InMemoryStackStore();
        store.TryCreate(NewStack(Coords("dev")));
        store.TryCreate(NewStack(Coords("prod")));

        // Move dev -> staging (free): succeeds.
        var moved = store.Rename(Coords("dev"), Coords("staging"), out var collided1);
        Assert.False(collided1);
        Assert.NotNull(moved);
        Assert.Null(store.Find(Coords("dev")));
        Assert.NotNull(store.Find(Coords("staging")));

        // Move staging -> prod (occupied): collision, no move.
        var blocked = store.Rename(Coords("staging"), Coords("prod"), out var collided2);
        Assert.True(collided2);
        Assert.Null(blocked);
        Assert.NotNull(store.Find(Coords("staging")));
    }
}
