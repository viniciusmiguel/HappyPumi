using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryCmkStore"/>: creating a key makes it the default and records a KEK
/// migration; a second create demotes the first; set-default demotes others and records a migration; disable
/// clears Enabled+IsDefault; disable-all reports the count; and retry flips failed migrations to completed.
/// </summary>
public sealed class InMemoryCmkStoreTests
{
    [Fact]
    public void CreateBecomesDefaultAppearsInListAndRecordsMigration()
    {
        var store = new InMemoryCmkStore();
        var key = store.Create("acme", "primary", "aws-kms", "arn:key", "arn:role");

        Assert.True(key.IsDefault);
        Assert.True(key.Enabled);
        Assert.Single(store.List("acme"));
        Assert.Single(store.ListMigrations("acme"));
    }

    [Fact]
    public void SecondCreateDemotesTheFirst()
    {
        var store = new InMemoryCmkStore();
        var first = store.Create("acme", "first", "aws-kms", "arn:1", null);
        var second = store.Create("acme", "second", "aws-kms", "arn:2", null);

        Assert.False(store.Get("acme", first.Id)!.IsDefault);
        Assert.True(store.Get("acme", second.Id)!.IsDefault);
        Assert.Equal(2, store.ListMigrations("acme").Count); // one per create
    }

    [Fact]
    public void SetDefaultDemotesOthersAndRecordsMigration()
    {
        var store = new InMemoryCmkStore();
        var first = store.Create("acme", "first", "aws-kms", "arn:1", null);
        var second = store.Create("acme", "second", "aws-kms", "arn:2", null);

        Assert.True(store.SetDefault("acme", first.Id));
        Assert.True(store.Get("acme", first.Id)!.IsDefault);
        Assert.False(store.Get("acme", second.Id)!.IsDefault);
        Assert.Equal(3, store.ListMigrations("acme").Count); // 2 creates + 1 set-default
    }

    [Fact]
    public void SetDefaultIsFalseForMissingKey()
    {
        var store = new InMemoryCmkStore();
        Assert.False(store.SetDefault("acme", "ghost"));
    }

    [Fact]
    public void DisableClearsEnabledAndIsDefault()
    {
        var store = new InMemoryCmkStore();
        var key = store.Create("acme", "primary", "aws-kms", "arn:1", null);

        Assert.True(store.Disable("acme", key.Id));
        var disabled = store.Get("acme", key.Id)!;
        Assert.False(disabled.Enabled);
        Assert.False(disabled.IsDefault);
    }

    [Fact]
    public void DisableIsFalseForMissingKey()
    {
        var store = new InMemoryCmkStore();
        Assert.False(store.Disable("acme", "ghost"));
    }

    [Fact]
    public void DisableAllDisablesEveryKeyAndReturnsCount()
    {
        var store = new InMemoryCmkStore();
        store.Create("acme", "a", "aws-kms", "arn:a", null);
        store.Create("acme", "b", "aws-kms", "arn:b", null);

        Assert.Equal(2, store.DisableAll("acme"));
        Assert.True(store.List("acme").All(k => !k.Enabled));
        Assert.Equal(0, store.DisableAll("acme")); // already disabled
    }

    [Fact]
    public void RetryMigrationsFlipsFailedToCompleted()
    {
        var store = new InMemoryCmkStore();
        store.Create("acme", "a", "aws-kms", "arn:a", null);
        var migration = store.ListMigrations("acme").Single();
        migration.State = "failed";

        Assert.Equal(1, store.RetryMigrations("acme"));
        Assert.Equal("completed", store.ListMigrations("acme").Single().State);
        Assert.Equal(0, store.RetryMigrations("acme")); // nothing left to flip
    }
}
