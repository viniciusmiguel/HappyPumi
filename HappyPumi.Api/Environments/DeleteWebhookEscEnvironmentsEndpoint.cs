// Implemented by hand (ESC webhooks). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>DeleteWebhook — removes a webhook (404 when absent).</summary>
public sealed class DeleteWebhookEscEnvironmentsEndpoint(IEnvironmentWebhookStore webhooks)
    : Endpoint<DeleteWebhookEscEnvironmentsRequest>
{
    public override void Configure()
    {
        Delete("/api/esc/environments/{orgName}/{projectName}/{envName}/hooks/{hookName}");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("DeleteWebhook")
            .WithName("DeleteWebhookEscEnvironments")
        );
    }

    public override async Task HandleAsync(DeleteWebhookEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (!webhooks.Delete(coords, req.HookName))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
