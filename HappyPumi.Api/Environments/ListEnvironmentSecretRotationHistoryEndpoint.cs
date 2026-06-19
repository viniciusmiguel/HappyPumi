// Implemented by hand (ESC rotation). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ListEnvironmentSecretRotationHistory — the environment's recorded rotation events (newest first).</summary>
public sealed class ListEnvironmentSecretRotationHistoryEndpoint(IEscRotationHistory history)
    : Endpoint<ListEnvironmentSecretRotationHistoryRequest, ListEnvironmentSecretRotationHistoryResponse>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/rotate/history");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ListEnvironmentSecretRotationHistory")
            .WithName("ListEnvironmentSecretRotationHistory")
        );
    }

    public override async Task HandleAsync(ListEnvironmentSecretRotationHistoryRequest req, CancellationToken ct)
    {
        var events = history.List(new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName)).ToList();
        await Send.OkAsync(new ListEnvironmentSecretRotationHistoryResponse { Events = events }, ct);
    }
}
