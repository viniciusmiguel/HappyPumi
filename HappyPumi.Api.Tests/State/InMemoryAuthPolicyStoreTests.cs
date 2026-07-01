using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryAuthPolicyStore"/> (policy-results PR2): a first upsert is version 1 and
/// readable; a second upsert replaces the rule set, bumps the version, and preserves the created timestamp;
/// an unset (org, policyId) reads back null.
/// </summary>
public sealed class InMemoryAuthPolicyStoreTests
{
    private static List<AuthPolicyDefinition> Rules(string decision)
        => new() { new AuthPolicyDefinition { Decision = decision, TokenType = "organization" } };

    [Fact]
    public void GetIsNullWhenNeverSet()
    {
        var store = new InMemoryAuthPolicyStore();
        Assert.Null(store.Get("acme", "issuer-1"));
    }

    [Fact]
    public void FirstUpsertIsVersionOneAndReadable()
    {
        var store = new InMemoryAuthPolicyStore();
        var created = store.Upsert("acme", "issuer-1", Rules("allow"));

        Assert.Equal(1, created.Version);
        var read = store.Get("acme", "issuer-1")!;
        Assert.Equal("allow", read.Policies[0].Decision);
    }

    [Fact]
    public void SecondUpsertBumpsVersionAndKeepsCreated()
    {
        var store = new InMemoryAuthPolicyStore();
        var first = store.Upsert("acme", "issuer-1", Rules("allow"));
        var second = store.Upsert("acme", "issuer-1", Rules("deny"));

        Assert.Equal(2, second.Version);
        Assert.Equal(first.Created, second.Created); // created is preserved across updates
        Assert.Equal("deny", store.Get("acme", "issuer-1")!.Policies[0].Decision);
    }
}
