// Implemented by hand (ESC revision tags). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;
using Microsoft.AspNetCore.Http;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>DeleteRevisionTag — removes a named revision tag. The built-in 'latest' tag cannot be deleted.</summary>
public sealed class DeleteRevisionTagEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<DeleteRevisionTagEscEnvironmentsRequest>
{
    public override void Configure()
    {
        Delete("/api/esc/environments/{orgName}/{projectName}/{envName}/versions/tags/{tagName}");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("DeleteRevisionTag")
            .WithName("DeleteRevisionTagEscEnvironments")
        );
    }

    public override async Task HandleAsync(DeleteRevisionTagEscEnvironmentsRequest req, CancellationToken ct)
    {
        if (req.TagName == "latest")
        {
            await Send.ResultAsync(Results.Conflict(new { message = "The built-in 'latest' revision tag cannot be deleted." }));
            return;
        }

        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (!environments.DeleteRevisionTag(coords, req.TagName))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
