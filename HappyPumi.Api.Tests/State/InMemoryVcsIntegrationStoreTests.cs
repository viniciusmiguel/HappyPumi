using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryVcsIntegrationStore"/>: the provider-neutral VCS integration-record
/// store (create / list / filter-by-kind / get / update-settings / delete).
/// </summary>
public sealed class InMemoryVcsIntegrationStoreTests
{
    private const string Org = "acme";

    private static StoredVcsIntegration Sample(string kind, string? account = null) => new()
    {
        Id = "ignored", // overwritten by Create
        Org = Org,
        Kind = kind,
        AccountName = account,
    };

    [Fact]
    public void CreateAssignsIdAndListReturnsIt()
    {
        var store = new InMemoryVcsIntegrationStore();
        var created = store.Create(Sample("github", "octo"));

        Assert.False(string.IsNullOrWhiteSpace(created.Id));
        Assert.NotEqual("ignored", created.Id);

        var all = store.List(Org);
        Assert.Single(all);
        Assert.Equal(created.Id, all[0].Id);
    }

    [Fact]
    public void ListFiltersByKind()
    {
        var store = new InMemoryVcsIntegrationStore();
        store.Create(Sample("github", "octo"));
        store.Create(Sample("azure-devops", "ado"));

        var gh = store.List(Org, "github");
        Assert.Single(gh);
        Assert.Equal("github", gh[0].Kind);
        Assert.Equal(2, store.List(Org).Count);
    }

    [Fact]
    public void GetReturnsStoredAndNullWhenMissing()
    {
        var store = new InMemoryVcsIntegrationStore();
        var created = store.Create(Sample("github"));

        Assert.Equal(created.Id, store.Get(Org, created.Id)!.Id);
        Assert.Null(store.Get(Org, "nope"));
        Assert.Null(store.Get("other-org", created.Id));
    }

    [Fact]
    public void UpdateSettingsMutatesAndReturns()
    {
        var store = new InMemoryVcsIntegrationStore();
        var created = store.Create(Sample("github"));

        var settings = new VcsIntegrationSettings { DisablePrComments = true, DisableNeoSummaries = true };
        var updated = store.UpdateSettings(Org, created.Id, settings);

        Assert.NotNull(updated);
        Assert.True(updated!.Settings.DisablePrComments);
        Assert.True(store.Get(Org, created.Id)!.Settings.DisableNeoSummaries);
        Assert.Null(store.UpdateSettings(Org, "nope", settings));
    }

    [Fact]
    public void DeleteRemovesThenReturnsFalse()
    {
        var store = new InMemoryVcsIntegrationStore();
        var created = store.Create(Sample("github"));

        Assert.True(store.Delete(Org, created.Id));
        Assert.Null(store.Get(Org, created.Id));
        Assert.False(store.Delete(Org, created.Id));
    }
}
