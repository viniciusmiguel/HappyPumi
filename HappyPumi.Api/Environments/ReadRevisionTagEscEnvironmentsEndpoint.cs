// Implemented by hand (ESC revision tags). The generator would overwrite this body; preserve it.
#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ReadRevisionTag — returns the revision a named tag points at, or 404 when it does not exist.</summary>
public sealed class ReadRevisionTagEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<ReadRevisionTagEscEnvironmentsRequest, EnvironmentRevisionTag>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/versions/tags/{tagName}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ReadRevisionTag")
            .WithName("ReadRevisionTagEscEnvironments")
        );
    }

    public override async Task HandleAsync(ReadRevisionTagEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var revision = environments.ListRevisions(coords).FirstOrDefault(r => r.Tags.Contains(req.TagName));
        if (revision is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(EscTagMapper.RevisionTag(revision, req.TagName), ct);
    }
}
