using System;
using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryChangeRequestEventStore"/>: append round-trips through List; the timeline
/// is returned oldest-first; events are scoped per (org, changeRequestId); an unknown CR lists empty.
/// </summary>
public sealed class InMemoryChangeRequestEventStoreTests
{
    private static StoredChangeRequestEvent Ev(string org, string cr, string type, DateTime at) => new()
    {
        Id = Guid.NewGuid().ToString(), Org = org, ChangeRequestId = cr, EventType = type,
        CreatedBy = "alice", CreatedAt = at,
    };

    [Fact]
    public void AppendListReturnsOldestFirst()
    {
        var store = new InMemoryChangeRequestEventStore();
        var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        store.Append(Ev("acme", "cr1", "status_changed", t0.AddMinutes(2)));
        store.Append(Ev("acme", "cr1", "commented", t0.AddMinutes(1)));
        store.Append(Ev("acme", "cr1", "description_updated", t0.AddMinutes(3)));

        var timeline = store.List("acme", "cr1").Select(e => e.EventType).ToArray();
        Assert.Equal(new[] { "commented", "status_changed", "description_updated" }, timeline);
    }

    [Fact]
    public void EventsAreScopedPerChangeRequest()
    {
        var store = new InMemoryChangeRequestEventStore();
        var now = DateTime.UtcNow;
        store.Append(Ev("acme", "cr1", "commented", now));
        store.Append(Ev("acme", "cr2", "commented", now));

        Assert.Single(store.List("acme", "cr1"));
        Assert.Empty(store.List("acme", "ghost"));
    }
}
