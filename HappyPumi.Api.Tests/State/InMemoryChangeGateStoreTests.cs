using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryChangeGateStore"/>: create round-trips through Get/List; Update mutates
/// in place and returns the gate; Get of an unknown id is null; Delete removes (true then false).
/// </summary>
public sealed class InMemoryChangeGateStoreTests
{
    private static StoredChangeGate NewGate(string org, string name) => new()
    {
        Id = Guid.NewGuid().ToString(), Org = org, Name = name, Enabled = true,
        NumApprovalsRequired = 2, ActionTypes = new() { "update" }, QualifiedName = "proj/env",
        EligibleApprovers = new() { new EligibleApprover { EligibilityType = "specific_user", UserLogin = "alice" } },
    };

    [Fact]
    public void CreateRoundTripsThroughGetAndList()
    {
        var store = new InMemoryChangeGateStore();
        var gate = store.Create(NewGate("acme", "prod-gate"));

        Assert.Equal(gate.Id, store.Get("acme", gate.Id)!.Id);
        Assert.Single(store.List("acme"));
        Assert.Equal("prod-gate", store.List("acme").Single().Name);
    }

    [Fact]
    public void UpdateMutatesAndReturnsGate()
    {
        var store = new InMemoryChangeGateStore();
        var gate = store.Create(NewGate("acme", "prod-gate"));

        var updated = store.Update("acme", gate.Id, g => { g.Enabled = false; g.NumApprovalsRequired = 5; });

        Assert.NotNull(updated);
        Assert.False(updated!.Enabled);
        Assert.Equal(5, store.Get("acme", gate.Id)!.NumApprovalsRequired);
    }

    [Fact]
    public void GetUnknownIsNull()
    {
        var store = new InMemoryChangeGateStore();
        Assert.Null(store.Get("acme", "ghost"));
        Assert.Null(store.Update("acme", "ghost", _ => { }));
    }

    [Fact]
    public void DeleteRemovesThenReportsFalse()
    {
        var store = new InMemoryChangeGateStore();
        var gate = store.Create(NewGate("acme", "prod-gate"));

        Assert.True(store.Delete("acme", gate.Id));
        Assert.Null(store.Get("acme", gate.Id));
        Assert.False(store.Delete("acme", gate.Id));
    }

    [Fact]
    public void ListIsScopedPerOrg()
    {
        var store = new InMemoryChangeGateStore();
        store.Create(NewGate("acme", "a"));
        store.Create(NewGate("other", "b"));

        Assert.Single(store.List("acme"));
        Assert.DoesNotContain(store.List("acme"), g => g.Name == "b");
    }
}
