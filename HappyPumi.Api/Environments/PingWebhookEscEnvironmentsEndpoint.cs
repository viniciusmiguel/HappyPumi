// Implemented by hand (ESC webhooks). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using HappyPumi.Api.Webhooks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>PingWebhook — sends a synthetic test event to the webhook and returns the delivery record.</summary>
public sealed class PingWebhookEscEnvironmentsEndpoint(WebhookDeliveryService deliveries)
    : Endpoint<PingWebhookEscEnvironmentsRequest, WebhookDelivery>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/hooks/{hookName}/ping");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("PingWebhook")
            .WithName("PingWebhookEscEnvironments")
        );
    }

    public override async Task HandleAsync(PingWebhookEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var delivery = await deliveries.PingAsync(coords, req.HookName, ct);
        if (delivery is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(delivery, ct);
    }
}
