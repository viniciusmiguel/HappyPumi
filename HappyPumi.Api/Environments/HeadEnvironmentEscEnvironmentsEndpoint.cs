// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;
using Microsoft.AspNetCore.Http;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>HeadEnvironment — a lightweight existence check (200 when the environment exists, else 404).</summary>
public sealed class HeadEnvironmentEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<HeadEnvironmentEscEnvironmentsRequest, object>
{
    public override void Configure()
    {
        Head("/api/esc/environments/{orgName}/{projectName}/{envName}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("HeadEnvironment")
            .WithName("HeadEnvironmentEscEnvironments")
        );
    }

    public override async Task HandleAsync(HeadEnvironmentEscEnvironmentsRequest req, CancellationToken ct)
    {
        var exists = environments.Get(new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName)) is not null;
        await Send.ResultAsync(exists ? Results.Ok() : Results.NotFound());
    }
}
