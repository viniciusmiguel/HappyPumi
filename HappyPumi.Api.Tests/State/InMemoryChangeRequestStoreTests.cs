using System;
using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryChangeRequestStore"/>: create round-trips through Get/List; Update
/// mutates in place and returns the record; Get/Update of an unknown id is null; List is scoped per org.
/// </summary>
public sealed class InMemoryChangeRequestStoreTests
{
    private static StoredChangeRequest NewCr(string org, string env) => new()
    {
        Id = Guid.NewGuid().ToString(), Org = org, TargetProject = "proj", TargetEnv = env,
        Description = "initial", CreatedBy = "alice", CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public void CreateRoundTripsThroughGetAndList()
    {
        var store = new InMemoryChangeRequestStore();
        var cr = store.Create(NewCr("acme", "prod"));

        Assert.Equal(cr.Id, store.Get("acme", cr.Id)!.Id);
        Assert.Single(store.List("acme"));
        Assert.Equal("draft", store.List("acme").Single().Status);
    }

    [Fact]
    public void UpdateMutatesAndReturnsRecord()
    {
        var store = new InMemoryChangeRequestStore();
        var cr = store.Create(NewCr("acme", "prod"));

        var updated = store.Update("acme", cr.Id, c =>
        {
            c.Status = "submitted";
            c.Approvers.Add("bob");
        });

        Assert.NotNull(updated);
        Assert.Equal("submitted", updated!.Status);
        Assert.Contains("bob", store.Get("acme", cr.Id)!.Approvers);
    }

    [Fact]
    public void GetAndUpdateUnknownAreNull()
    {
        var store = new InMemoryChangeRequestStore();
        Assert.Null(store.Get("acme", "ghost"));
        Assert.Null(store.Update("acme", "ghost", _ => { }));
    }

    [Fact]
    public void ListIsScopedPerOrg()
    {
        var store = new InMemoryChangeRequestStore();
        store.Create(NewCr("acme", "a"));
        store.Create(NewCr("other", "b"));

        Assert.Single(store.List("acme"));
        Assert.DoesNotContain(store.List("acme"), c => c.TargetEnv == "b");
    }
}
