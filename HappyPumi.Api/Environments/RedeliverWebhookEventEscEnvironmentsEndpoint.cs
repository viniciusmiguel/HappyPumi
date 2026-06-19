// Implemented by hand (ESC webhooks). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using HappyPumi.Api.Webhooks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>RedeliverWebhookEvent — re-sends a recorded delivery's payload (404 when the delivery is unknown).</summary>
public sealed class RedeliverWebhookEventEscEnvironmentsEndpoint(WebhookDeliveryService deliveries)
    : Endpoint<RedeliverWebhookEventEscEnvironmentsRequest, WebhookDelivery>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/hooks/{hookName}/deliveries/{event}/redeliver");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("RedeliverWebhookEvent")
            .WithName("RedeliverWebhookEventEscEnvironments")
        );
    }

    public override async Task HandleAsync(RedeliverWebhookEventEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var delivery = await deliveries.RedeliverAsync(coords, req.HookName, req.Event, ct);
        if (delivery is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(delivery, ct);
    }
}
