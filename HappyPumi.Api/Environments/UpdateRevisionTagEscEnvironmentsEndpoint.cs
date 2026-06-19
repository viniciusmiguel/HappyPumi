// Implemented by hand (ESC revision tags). The generator would overwrite this body; preserve it.
#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>UpdateRevisionTag — moves an existing named tag to a different revision (e.g. advance 'prod').</summary>
public sealed class UpdateRevisionTagEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<UpdateRevisionTagEscEnvironmentsRequest>
{
    public override void Configure()
    {
        Patch("/api/esc/environments/{orgName}/{projectName}/{envName}/versions/tags/{tagName}");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("UpdateRevisionTag")
            .WithName("UpdateRevisionTagEscEnvironments")
        );
    }

    public override async Task HandleAsync(UpdateRevisionTagEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (req.Body?.Revision is null)
        {
            AddError("'revision' is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var exists = environments.ListRevisions(coords).Any(r => r.Tags.Contains(req.TagName));
        if (!exists || environments.SetRevisionTag(coords, req.TagName, req.Body.Revision.Value) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
