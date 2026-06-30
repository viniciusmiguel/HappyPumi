using System;
using System.Threading;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for the in-memory <see cref="IWebhookDeliveryStore"/> (ADR-0005). Each test uses a fresh
/// instance; covers append/list filtering by (scope, name) and latest-by-event for redeliver.
/// </summary>
public sealed class InMemoryWebhookDeliveryStoreTests
{
    private static readonly WebhookScope Stack = new("stack", "org/proj/dev");

    private static StoredWebhookDelivery Delivery(string id, string @event, WebhookScope? scope = null, string name = "ci")
        => new()
        {
            Id = id, Scope = scope ?? Stack, WebhookName = name, Event = @event,
            RequestBody = "{}", ResponseStatus = 200, Timestamp = DateTime.UtcNow,
        };

    [Fact]
    public void AppendThenListReturnsTheDelivery()
    {
        var store = new InMemoryWebhookDeliveryStore();
        var d = store.Append(Delivery("d1", "stack_update"));

        var listed = store.List(Stack, "ci");

        Assert.Single(listed);
        Assert.Equal(d.Id, listed[0].Id);
    }

    [Fact]
    public void ListFiltersByScopeAndWebhookName()
    {
        var store = new InMemoryWebhookDeliveryStore();
        store.Append(Delivery("d1", "stack_update", name: "ci"));
        store.Append(Delivery("d2", "stack_update", name: "other"));
        store.Append(Delivery("d3", "stack_update", scope: new WebhookScope("org", "org")));

        var listed = store.List(Stack, "ci");

        Assert.Single(listed);
        Assert.Equal("d1", listed[0].Id);
    }

    [Fact]
    public void LatestByEventReturnsNewestMatchOrNull()
    {
        var store = new InMemoryWebhookDeliveryStore();
        store.Append(new StoredWebhookDelivery
        {
            Id = "old", Scope = Stack, WebhookName = "ci", Event = "stack_update",
            Timestamp = DateTime.UtcNow.AddMinutes(-5),
        });
        Thread.Sleep(1);
        store.Append(new StoredWebhookDelivery
        {
            Id = "new", Scope = Stack, WebhookName = "ci", Event = "stack_update",
            Timestamp = DateTime.UtcNow,
        });

        Assert.Equal("new", store.LatestByEvent(Stack, "ci", "stack_update")!.Id);
        Assert.Null(store.LatestByEvent(Stack, "ci", "deployment_succeeded"));
    }
}
