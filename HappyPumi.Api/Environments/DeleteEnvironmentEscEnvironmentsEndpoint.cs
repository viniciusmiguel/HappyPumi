// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// DeleteEnvironment — removes an environment and its revision history. Blocked (409) when deletion
/// protection is enabled on the environment's settings.
/// </summary>
public sealed class DeleteEnvironmentEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<DeleteEnvironmentEscEnvironmentsRequest>
{
    public override void Configure()
    {
        Delete("/api/esc/environments/{orgName}/{projectName}/{envName}");
        Permissions("environment:delete");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("DeleteEnvironment")
            .WithName("DeleteEnvironmentEscEnvironments")
        );
    }

    public override async Task HandleAsync(DeleteEnvironmentEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var env = environments.Get(coords);
        if (env is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        if (env.DeletionProtected)
        {
            await Send.ResultAsync(Microsoft.AspNetCore.Http.Results.Conflict(
                new { message = $"Environment '{req.ProjectName}/{req.EnvName}' is deletion-protected." }));
            return;
        }

        environments.Delete(coords);
        await Send.NoContentAsync(ct);
    }
}
