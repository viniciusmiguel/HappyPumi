using System;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryTemplateSourceStore"/>: create round-trips through Get/List; Update
/// mutates in place and returns the source; Get of an unknown id is null; Delete removes (true then false).
/// </summary>
public sealed class InMemoryTemplateSourceStoreTests
{
    private static StoredTemplateSource NewSource(string org, string name) => new()
    {
        Id = Guid.NewGuid().ToString(), Org = org, Name = name,
        SourceUrl = "https://example.com/templates.git", IsValid = true,
    };

    [Fact]
    public void CreateRoundTripsThroughGetAndList()
    {
        var store = new InMemoryTemplateSourceStore();
        var source = store.Create(NewSource("acme", "team-templates"));

        Assert.Equal(source.Id, store.Get("acme", source.Id)!.Id);
        Assert.Single(store.List("acme"));
        Assert.Equal("team-templates", store.List("acme").Single().Name);
    }

    [Fact]
    public void UpdateMutatesAndReturnsSource()
    {
        var store = new InMemoryTemplateSourceStore();
        var source = store.Create(NewSource("acme", "team-templates"));

        var updated = store.Update("acme", source.Id, s => { s.Name = "renamed"; s.IsValid = false; });

        Assert.NotNull(updated);
        Assert.Equal("renamed", updated!.Name);
        Assert.False(store.Get("acme", source.Id)!.IsValid);
    }

    [Fact]
    public void GetUnknownIsNull()
    {
        var store = new InMemoryTemplateSourceStore();
        Assert.Null(store.Get("acme", "ghost"));
        Assert.Null(store.Update("acme", "ghost", _ => { }));
    }

    [Fact]
    public void DeleteRemovesThenReportsFalse()
    {
        var store = new InMemoryTemplateSourceStore();
        var source = store.Create(NewSource("acme", "team-templates"));

        Assert.True(store.Delete("acme", source.Id));
        Assert.Null(store.Get("acme", source.Id));
        Assert.False(store.Delete("acme", source.Id));
    }

    [Fact]
    public void ListIsScopedPerOrg()
    {
        var store = new InMemoryTemplateSourceStore();
        store.Create(NewSource("acme", "a"));
        store.Create(NewSource("other", "b"));

        Assert.Single(store.List("acme"));
        Assert.DoesNotContain(store.List("acme"), s => s.Name == "b");
    }
}
