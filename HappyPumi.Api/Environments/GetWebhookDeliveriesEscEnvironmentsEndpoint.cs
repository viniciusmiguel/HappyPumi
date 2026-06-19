// Implemented by hand (ESC webhooks). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using HappyPumi.Api.Webhooks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>GetWebhookDeliveries — recent delivery attempts for a webhook (newest first).</summary>
public sealed class GetWebhookDeliveriesEscEnvironmentsEndpoint(IWebhookDeliveryLog log)
    : Endpoint<GetWebhookDeliveriesEscEnvironmentsRequest, List<WebhookDelivery>>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/hooks/{hookName}/deliveries");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("GetWebhookDeliveries")
            .WithName("GetWebhookDeliveriesEscEnvironments")
        );
    }

    public override async Task HandleAsync(GetWebhookDeliveriesEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        await Send.OkAsync(log.List(coords, req.HookName).ToList(), ct);
    }
}
