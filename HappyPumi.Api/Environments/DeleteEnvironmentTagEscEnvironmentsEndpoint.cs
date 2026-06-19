// Implemented by hand (ESC tags). The generator would overwrite this body; preserve it.
#nullable enable

using FastEndpoints;
using HappyPumi.Api.State;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>DeleteEnvironmentTag — removes a tag (404 when the environment or tag does not exist).</summary>
public sealed class DeleteEnvironmentTagEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<DeleteEnvironmentTagEscEnvironmentsRequest>
{
    public override void Configure()
    {
        Delete("/api/esc/environments/{orgName}/{projectName}/{envName}/tags/{tagName}");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("DeleteEnvironmentTag")
            .WithName("DeleteEnvironmentTagEscEnvironments")
        );
    }

    public override async Task HandleAsync(DeleteEnvironmentTagEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (!environments.DeleteTag(coords, req.TagName))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
