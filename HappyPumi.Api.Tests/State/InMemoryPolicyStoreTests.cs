using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>Unit tests for the in-memory CrossGuard policy store (groups + versioned packs; ENDPOINTS.md 5).</summary>
public sealed class InMemoryPolicyStoreTests
{
    private const string Org = "acme";

    [Fact]
    public void GroupCreateGetRenameDelete()
    {
        var store = new InMemoryPolicyStore();

        Assert.NotNull(store.NewGroup(Org, "prod"));
        Assert.Null(store.NewGroup(Org, "prod"));            // duplicate
        Assert.NotNull(store.GetGroup(Org, "prod"));

        Assert.True(store.RenameGroup(Org, "prod", "production"));
        Assert.Null(store.GetGroup(Org, "prod"));
        Assert.NotNull(store.GetGroup(Org, "production"));

        Assert.True(store.DeleteGroup(Org, "production"));
        Assert.False(store.DeleteGroup(Org, "production"));
    }

    [Fact]
    public void PackVersionsAutoIncrementAndComplete()
    {
        var store = new InMemoryPolicyStore();

        Assert.Equal(1, store.CreatePackVersion(Org, "sec", "Security", null));
        Assert.Equal(2, store.CreatePackVersion(Org, "sec", "Security", null));

        Assert.True(store.CompletePack(Org, "sec", 2));
        Assert.True(store.GetPack(Org, "sec")!.Versions[2].Published);
        Assert.False(store.CompletePack(Org, "sec", 99));
    }

    [Fact]
    public void DeletePackAndVersion()
    {
        var store = new InMemoryPolicyStore();
        store.CreatePackVersion(Org, "sec", "Security", null);
        store.CreatePackVersion(Org, "sec", "Security", null);

        Assert.True(store.DeletePackVersion(Org, "sec", 1));
        Assert.False(store.GetPack(Org, "sec")!.Versions.ContainsKey(1));

        Assert.True(store.DeletePack(Org, "sec"));
        Assert.Null(store.GetPack(Org, "sec"));
    }
}
