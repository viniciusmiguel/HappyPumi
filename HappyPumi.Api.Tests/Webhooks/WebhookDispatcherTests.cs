using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using HappyPumi.Api.Tests.Esc;
using HappyPumi.Api.Webhooks;
using Microsoft.Extensions.Configuration;

namespace HappyPumi.Api.Tests.Webhooks;

/// <summary>
/// Unit tests for <see cref="WebhookDispatcher"/> driven by the ESC <see cref="StubHttpHandler"/> (no network):
/// filter matching, the outbound POST shape (URL, signature, formatted body), recorded delivery status, the
/// SSRF deny-list, and the Slack format. The in-memory delivery store records what the dispatcher appends.
/// </summary>
public sealed class WebhookDispatcherTests
{
    private static readonly WebhookScope Scope = new("stack", "org/proj/dev");

    private static WebhookDispatcher Dispatcher(StubHttpHandler handler, IWebhookDeliveryStore store, string? blockedHosts = null)
    {
        var settings = new Dictionary<string, string?>();
        if (blockedHosts is not null)
            settings["Webhooks:BlockedHosts"] = blockedHosts;
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        IWebhookPayloadFormatter[] formatters =
            [new RawFormatter(), new SlackFormatter(), new MsTeamsFormatter(), new PulumiDeploymentsFormatter()];
        return new WebhookDispatcher(new HttpClient(handler), store, formatters, config);
    }

    private static WebhookResponse Hook(string name = "ci", string url = "https://hooks.test/x",
        bool active = true, List<string>? filters = null, string? format = null, string? secret = null)
        => new() { Name = name, PayloadUrl = url, Active = active, Filters = filters, Format = format, Secret = secret };

    [Fact]
    public async Task FiresOnlyToMatchingActiveWebhooks()
    {
        var handler = new StubHttpHandler("{}");
        var store = new InMemoryWebhookDeliveryStore();
        var webhooks = new[]
        {
            Hook("match", filters: ["stack_update"]),
            Hook("other", filters: ["deployment_succeeded"]),
            Hook("inactive", active: false, filters: ["stack_update"]),
        };

        var fired = await Dispatcher(handler, store).FireAsync(Scope, webhooks, "stack_update", new { }, CancellationToken.None);

        Assert.Single(fired);
        Assert.Equal("match", fired[0].WebhookName);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task PostsFormattedBodyWithSignatureHeaderToPayloadUrl()
    {
        var handler = new StubHttpHandler("{}");
        var store = new InMemoryWebhookDeliveryStore();
        var hook = Hook(url: "https://hooks.test/deliver", secret: "s3cr3t");

        await Dispatcher(handler, store).FireAsync(Scope, [hook], "stack_update", new { kind = "stack_update" }, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://hooks.test/deliver", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("stack_update", handler.LastBody);
        var header = handler.LastRequest.Headers.GetValues(WebhookSignature.HeaderName).Single();
        Assert.Equal(WebhookSignature.Sign(handler.LastBody!, "s3cr3t"), header);
    }

    [Fact]
    public async Task RecordsDeliveryWithResponseStatus()
    {
        var handler = new StubHttpHandler("ok", HttpStatusCode.Accepted);
        var store = new InMemoryWebhookDeliveryStore();

        var fired = await Dispatcher(handler, store).FireAsync(Scope, [Hook()], "stack_update", new { }, CancellationToken.None);

        Assert.Equal(202, fired[0].ResponseStatus);
        Assert.Single(store.List(Scope, "ci"));
        Assert.Equal(fired[0].Id, store.List(Scope, "ci")[0].Id);
    }

    [Fact]
    public async Task BlockedHostRecordsStatusZeroAndDoesNotPost()
    {
        var handler = new StubHttpHandler("{}");
        var store = new InMemoryWebhookDeliveryStore();
        var hook = Hook(url: "https://metadata.internal/latest");

        var fired = await Dispatcher(handler, store, blockedHosts: "metadata.internal")
            .FireAsync(Scope, [hook], "stack_update", new { }, CancellationToken.None);

        Assert.Equal(0, fired[0].ResponseStatus);
        Assert.Contains("blocked", fired[0].ResponseBody);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task SlackFormatRendersSlackShape()
    {
        var handler = new StubHttpHandler("{}");
        var store = new InMemoryWebhookDeliveryStore();

        await Dispatcher(handler, store).FireAsync(Scope, [Hook(format: "slack")], "stack_update", new { }, CancellationToken.None);

        Assert.Contains("\"text\"", handler.LastBody);
        Assert.Contains("attachments", handler.LastBody);
    }
}
