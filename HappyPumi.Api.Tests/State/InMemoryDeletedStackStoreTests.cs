using System;
using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryDeletedStackStore"/>: Record round-trips through Get/List; List is
/// newest-first and scoped per org; Remove reports true then false.
/// </summary>
public sealed class InMemoryDeletedStackStoreTests
{
    private static StoredDeletedStack NewTombstone(string org, string project, string stack, long deletedAt) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Org = org,
        ProjectName = project,
        StackName = stack,
        ProgramId = Guid.NewGuid().ToString("N"),
        Version = 3,
        DeletedAtUnix = deletedAt,
    };

    [Fact]
    public void RecordRoundTripsThroughGetAndList()
    {
        var store = new InMemoryDeletedStackStore();
        var tombstone = store.Record(NewTombstone("acme", "web", "dev", 100));

        var fetched = store.Get("acme", tombstone.ProgramId);
        Assert.NotNull(fetched);
        Assert.Equal("dev", fetched!.StackName);
        Assert.Equal(3, fetched.Version);
        Assert.Single(store.List("acme"));
    }

    [Fact]
    public void ListIsNewestFirstAndScopedPerOrg()
    {
        var store = new InMemoryDeletedStackStore();
        store.Record(NewTombstone("acme", "web", "old", 100));
        store.Record(NewTombstone("acme", "web", "new", 200));
        store.Record(NewTombstone("other", "web", "x", 300));

        var acme = store.List("acme");
        Assert.Equal(2, acme.Count);
        Assert.Equal("new", acme.First().StackName); // newest deletion first
        Assert.DoesNotContain(acme, t => t.StackName == "x");
    }

    [Fact]
    public void RemoveReportsTrueThenFalse()
    {
        var store = new InMemoryDeletedStackStore();
        var tombstone = store.Record(NewTombstone("acme", "web", "dev", 100));

        Assert.True(store.Remove("acme", tombstone.ProgramId));
        Assert.Null(store.Get("acme", tombstone.ProgramId));
        Assert.False(store.Remove("acme", tombstone.ProgramId));
    }
}
