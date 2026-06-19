using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.State;
using HappyPumi.Api.Webhooks;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for webhook ping/redeliver (fake sender + fake store).</summary>
public sealed class WebhookDeliveryServiceTests
{
    private static readonly EnvCoordinates Env = new("happypumi", "proj", "app");

    private static (WebhookDeliveryService Service, FakeWebhookSender Sender, IWebhookDeliveryLog Log) Build()
    {
        var store = new FakeEnvironmentWebhookStore().With(Env,
            new StoredWebhook { Name = "ci", PayloadUrl = "https://hooks.example.com/ci", Secret = "s3cr3t" });
        var sender = new FakeWebhookSender();
        var log = new FakeWebhookDeliveryLog();
        return (new WebhookDeliveryService(store, sender, log), sender, log);
    }

    [Fact]
    public async Task PingSendsRecordsAndReturnsDelivery()
    {
        var (service, sender, log) = Build();

        var delivery = await service.PingAsync(Env, "ci", CancellationToken.None);

        Assert.Equal("ping", delivery!.Kind);
        Assert.Equal(200, delivery.ResponseCode);
        Assert.Equal("https://hooks.example.com/ci", delivery.RequestUrl);
        Assert.Equal("s3cr3t", sender.Sends.Single().Secret);        // secret passed to the sender for signing
        Assert.Equal(delivery.Id, log.List(Env, "ci").Single().Id);  // recorded
    }

    [Fact]
    public async Task RedeliverResendsPriorPayload()
    {
        var (service, sender, log) = Build();
        var first = await service.PingAsync(Env, "ci", CancellationToken.None);

        var redelivered = await service.RedeliverAsync(Env, "ci", first!.Id, CancellationToken.None);

        Assert.Equal(first.Payload, redelivered!.Payload); // same payload re-sent
        Assert.Equal(2, sender.Sends.Count);
        Assert.Equal(2, log.List(Env, "ci").Count);
    }

    [Fact]
    public async Task PingMissingWebhookReturnsNull()
    {
        var (service, _, _) = Build();
        Assert.Null(await service.PingAsync(Env, "nope", CancellationToken.None));
    }

    [Fact]
    public async Task RedeliverUnknownDeliveryReturnsNull()
    {
        var (service, _, _) = Build();
        Assert.Null(await service.RedeliverAsync(Env, "ci", "bogus", CancellationToken.None));
    }
}
