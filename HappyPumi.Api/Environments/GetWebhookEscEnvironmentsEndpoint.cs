// Implemented by hand (ESC webhooks). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>GetWebhook — a single webhook by name (404 when absent).</summary>
public sealed class GetWebhookEscEnvironmentsEndpoint(IEnvironmentWebhookStore webhooks)
    : Endpoint<GetWebhookEscEnvironmentsRequest, WebhookResponse>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/hooks/{hookName}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("GetWebhook")
            .WithName("GetWebhookEscEnvironments")
        );
    }

    public override async Task HandleAsync(GetWebhookEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var webhook = webhooks.Get(coords, req.HookName);
        if (webhook is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(WebhookMapper.ToResponse(webhook, coords), ct);
    }
}
