// Implemented by hand (ESC open-request workflow). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>CreateOpenEnvironmentRequest — opens an access request for a gated environment (404 when missing).</summary>
public sealed class CreateOpenEnvironmentRequestEndpoint(IEnvironmentStore environments, IEscOpenRequestStore requests)
    : Endpoint<CreateOpenEnvironmentRequestRequest, CreateEnvironmentOpenRequestResponse>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/open/request");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("CreateOpenEnvironmentRequest")
            .WithName("CreateOpenEnvironmentRequest")
        );
    }

    public override async Task HandleAsync(CreateOpenEnvironmentRequestRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var env = environments.Get(coords);
        if (env is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var requester = User.Identity?.Name ?? "happypumi";
        var request = requests.Create(coords, req.Body?.AccessDurationSeconds ?? 0, req.Body?.GrantExpirationSeconds ?? 0, env.CurrentRevision, requester);
        var result = new CreateEnvironmentOpenRequestResult
        {
            ChangeRequestId = request.Id, EnvironmentName = env.Coordinates.Name, ProjectName = env.Coordinates.Project,
            Etag = "", LatestRevisionNumber = env.CurrentRevision,
        };
        await Send.OkAsync(new CreateEnvironmentOpenRequestResponse { ChangeRequests = new List<CreateEnvironmentOpenRequestResult> { result } }, ct);
    }
}
