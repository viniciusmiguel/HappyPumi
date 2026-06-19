// Implemented by hand (ESC webhooks). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ListWebhooks — the webhooks configured on an environment.</summary>
public sealed class ListWebhooksEscEnvironmentsEndpoint(IEnvironmentWebhookStore webhooks)
    : Endpoint<ListWebhooksEscEnvironmentsRequest, List<WebhookResponse>>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/hooks");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ListWebhooks")
            .WithName("ListWebhooksEscEnvironments")
        );
    }

    public override async Task HandleAsync(ListWebhooksEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var response = webhooks.List(coords).Select(w => WebhookMapper.ToResponse(w, coords)).ToList();
        await Send.OkAsync(response, ct);
    }
}
