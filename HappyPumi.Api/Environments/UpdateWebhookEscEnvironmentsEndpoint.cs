// Implemented by hand (ESC webhooks). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>UpdateWebhook — replaces a webhook's settings (404 when absent). The secret is kept if omitted.</summary>
public sealed class UpdateWebhookEscEnvironmentsEndpoint(IEnvironmentWebhookStore webhooks)
    : Endpoint<UpdateWebhookEscEnvironmentsRequest, WebhookResponse>
{
    public override void Configure()
    {
        Patch("/api/esc/environments/{orgName}/{projectName}/{envName}/hooks/{hookName}");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("UpdateWebhook")
            .WithName("UpdateWebhookEscEnvironments")
        );
    }

    public override async Task HandleAsync(UpdateWebhookEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var updated = webhooks.Update(coords, req.HookName, WebhookMapper.FromContract(req.Body));
        if (updated is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(WebhookMapper.ToResponse(updated, coords), ct);
    }
}
