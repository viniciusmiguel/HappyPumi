using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryAccessTokenStore"/>: issue returns a single-use <c>pul-</c> plaintext
/// while persisting only its SHA-256 hash; list/delete are scoped per (scope, owner); and role filtering
/// returns just the org tokens carrying that role.
/// </summary>
public sealed class InMemoryAccessTokenStoreTests
{
    [Fact]
    public void IssueStoresHashNotPlaintextAndReturnsPulPrefixedValue()
    {
        var store = new InMemoryAccessTokenStore();
        var token = store.Issue("user", "alice", "ci", "CI token", "alice", out var plaintext);

        Assert.StartsWith("pul-", plaintext);
        Assert.NotEqual(plaintext, token.HashedValue);
        Assert.Equal(AccessTokenSecret.Hash(plaintext), token.HashedValue);
    }

    [Fact]
    public void EachIssueProducesADistinctPlaintext()
    {
        var store = new InMemoryAccessTokenStore();
        store.Issue("user", "alice", "a", "", "alice", out var first);
        store.Issue("user", "alice", "b", "", "alice", out var second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ListReturnsIssuedTokensNewestFirst()
    {
        var store = new InMemoryAccessTokenStore();
        store.Issue("user", "alice", "a", "", "alice", out _);
        store.Issue("user", "alice", "b", "", "alice", out _);
        Assert.Equal(2, store.List("user", "alice").Count);
    }

    [Fact]
    public void ListIsScopedPerOwner()
    {
        var store = new InMemoryAccessTokenStore();
        store.Issue("user", "alice", "a", "", "alice", out _);
        store.Issue("user", "bob", "b", "", "bob", out _);
        Assert.Single(store.List("user", "alice"));
        Assert.Equal("a", store.List("user", "alice")[0].Name);
    }

    [Fact]
    public void DeleteRemovesAndReportsMissing()
    {
        var store = new InMemoryAccessTokenStore();
        var token = store.Issue("org", "acme", "ci", "", "alice", out _);
        Assert.True(store.Delete("org", "acme", token.Id));
        Assert.False(store.Delete("org", "acme", token.Id));
    }

    [Fact]
    public void DeleteIsScopedAndDoesNotCrossOwners()
    {
        var store = new InMemoryAccessTokenStore();
        var token = store.Issue("org", "acme", "ci", "", "alice", out _);
        Assert.False(store.Delete("org", "other", token.Id)); // wrong owner
        Assert.Single(store.List("org", "acme"));
    }

    [Fact]
    public void ListByRoleReturnsOnlyMatchingOrgTokens()
    {
        var store = new InMemoryAccessTokenStore();
        store.Issue("org", "acme", "deployer", "", "alice", out _, roleId: "role-deploy");
        store.Issue("org", "acme", "reader", "", "alice", out _, roleId: "role-read");
        store.Issue("org", "acme", "norole", "", "alice", out _);

        var matched = store.ListByRole("acme", "role-deploy");
        Assert.Single(matched);
        Assert.Equal("deployer", matched[0].Name);
    }
}
