// Implemented by hand (ESC open-request workflow). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>UpdateOpenEnvironmentRequest — updates an access request's durations (404 when unknown).</summary>
public sealed class UpdateOpenEnvironmentRequestEndpoint(IEscOpenRequestStore requests)
    : Endpoint<UpdateOpenEnvironmentRequestRequest, ChangeRequestRef>
{
    public override void Configure()
    {
        Put("/api/esc/environments/{orgName}/{projectName}/{envName}/open/request/{changeRequestID}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("UpdateOpenEnvironmentRequest")
            .WithName("UpdateOpenEnvironmentRequest")
        );
    }

    public override async Task HandleAsync(UpdateOpenEnvironmentRequestRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var updated = requests.Update(coords, req.ChangeRequestId, req.Body?.AccessDurationSeconds ?? 0, req.Body?.GrantExpirationSeconds ?? 0);
        if (updated is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(new ChangeRequestRef { ChangeRequestId = updated.Id, LatestRevisionNumber = updated.BaseRevision }, ct);
    }
}
