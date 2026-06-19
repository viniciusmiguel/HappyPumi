// Implemented by hand (ESC webhooks). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using Microsoft.AspNetCore.Http;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>CreateWebhook — adds a webhook to an environment (409 when the name is already in use).</summary>
public sealed class CreateWebhookEscEnvironmentsEndpoint(IEnvironmentStore environments, IEnvironmentWebhookStore webhooks)
    : Endpoint<CreateWebhookEscEnvironmentsRequest, WebhookResponse>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/hooks");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("CreateWebhook")
            .WithName("CreateWebhookEscEnvironments")
        );
    }

    public override async Task HandleAsync(CreateWebhookEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (environments.Get(coords) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        if (string.IsNullOrWhiteSpace(req.Body?.Name) || string.IsNullOrWhiteSpace(req.Body.PayloadUrl))
        {
            AddError("'name' and 'payloadUrl' are required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var created = webhooks.Create(coords, WebhookMapper.FromContract(req.Body));
        if (created is null)
        {
            await Send.ResultAsync(Results.Conflict(new { message = $"Webhook '{req.Body.Name}' already exists." }));
            return;
        }
        await Send.OkAsync(WebhookMapper.ToResponse(created, coords), ct);
    }
}
