using System.Collections.Generic;
using HappyPumi.Api.State;
using Xunit;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryConnectedCloudAccountStore"/>: per-(org,provider) round-trip, upsert
/// replaces the stored accounts, and List is empty when nothing is connected.
/// </summary>
public sealed class InMemoryConnectedCloudAccountStoreTests
{
    private static CloudAccountEntry Entry(string id, string name, long? number = null)
        => new() { Id = id, Name = name, Number = number, Roles = new List<string> { "admin" } };

    [Fact]
    public void UpsertThenListRoundTripsPerOrgAndProvider()
    {
        var store = new InMemoryConnectedCloudAccountStore();

        store.Upsert("acme", "azure", new[] { Entry("s1", "Sub One") }, credential: "tok");
        store.Upsert("acme", "gcp", new[] { Entry("p1", "Project One", 42) }, credential: null);

        var azure = Assert.Single(store.List("acme", "azure"));
        Assert.Equal("s1", azure.Id);
        Assert.Equal("Sub One", azure.Name);

        var gcp = Assert.Single(store.List("acme", "gcp"));
        Assert.Equal("p1", gcp.Id);
        Assert.Equal(42, gcp.Number);
    }

    [Fact]
    public void UpsertReplacesExistingAccounts()
    {
        var store = new InMemoryConnectedCloudAccountStore();

        store.Upsert("acme", "azure", new[] { Entry("s1", "Sub One") }, credential: "tok");
        store.Upsert("acme", "azure", new[] { Entry("s2", "Sub Two"), Entry("s3", "Sub Three") }, credential: "tok2");

        var accounts = store.List("acme", "azure");
        Assert.Equal(2, accounts.Count);
        Assert.DoesNotContain(accounts, a => a.Id == "s1");
    }

    [Fact]
    public void ListIsEmptyWhenNothingConnected()
        => Assert.Empty(new InMemoryConnectedCloudAccountStore().List("acme", "aws"));
}
