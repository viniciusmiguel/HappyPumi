using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>Unit tests for the in-memory agent-pool store, focused on the Delete/Update extensions (PR3).</summary>
public sealed class InMemoryAgentPoolStoreTests
{
    private const string Org = "acme";

    [Fact]
    public void DeletePoolRemovesExistingAndReportsMissing()
    {
        var store = new InMemoryAgentPoolStore();
        var pool = store.CreatePool(Org, "runners", "self-hosted");

        Assert.True(store.DeletePool(Org, pool.Id));
        Assert.Null(store.GetPool(Org, pool.Id));
        Assert.False(store.DeletePool(Org, pool.Id));   // second delete: already gone
        Assert.False(store.DeletePool(Org, "ghost"));   // never existed
    }

    [Fact]
    public void DeletePoolIsScopedToOrg()
    {
        var store = new InMemoryAgentPoolStore();
        var pool = store.CreatePool(Org, "runners", "");

        Assert.False(store.DeletePool("other-org", pool.Id));
        Assert.NotNull(store.GetPool(Org, pool.Id));
    }

    [Fact]
    public void UpdatePoolMutatesSuppliedFieldsOnly()
    {
        var store = new InMemoryAgentPoolStore();
        var pool = store.CreatePool(Org, "runners", "original");

        var renamed = store.UpdatePool(Org, pool.Id, "renamed", null);

        Assert.NotNull(renamed);
        Assert.Equal("renamed", renamed!.Name);
        Assert.Equal("original", renamed.Description); // null description leaves it unchanged
    }

    [Fact]
    public void UpdatePoolReturnsNullWhenMissing()
    {
        var store = new InMemoryAgentPoolStore();

        Assert.Null(store.UpdatePool(Org, "ghost", "x", "y"));
    }
}
