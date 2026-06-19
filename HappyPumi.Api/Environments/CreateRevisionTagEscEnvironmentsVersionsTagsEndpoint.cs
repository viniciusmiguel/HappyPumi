// Implemented by hand (ESC revision tags). The generator would overwrite this body; preserve it.
#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using Microsoft.AspNetCore.Http;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>CreateRevisionTag — points a new named tag at a revision (409 if the name is already in use).</summary>
public sealed class CreateRevisionTagEscEnvironmentsVersionsTagsEndpoint(IEnvironmentStore environments)
    : Endpoint<CreateRevisionTagEscEnvironmentsVersionsTagsRequest, EnvironmentRevisionTag>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/versions/tags");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("CreateRevisionTag")
            .WithName("CreateRevisionTagEscEnvironmentsVersionsTags")
        );
    }

    public override async Task HandleAsync(CreateRevisionTagEscEnvironmentsVersionsTagsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (string.IsNullOrWhiteSpace(req.Body?.Name) || req.Body.Revision is null)
        {
            AddError("'name' and 'revision' are required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var revisions = environments.ListRevisions(coords);
        if (revisions.Any(r => r.Tags.Contains(req.Body.Name)))
        {
            await Send.ResultAsync(Results.Conflict(new { message = $"Revision tag '{req.Body.Name}' already exists." }));
            return;
        }

        var revision = environments.SetRevisionTag(coords, req.Body.Name, req.Body.Revision.Value);
        if (revision is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(EscTagMapper.RevisionTag(revision, req.Body.Name), ct);
    }
}
