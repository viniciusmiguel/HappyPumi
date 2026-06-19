// Implemented by hand (ESC revision tags). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// ListRevisionTags (per version) — the revision tags pointing at one revision. The <c>version</c> path
/// segment may be a revision number or an existing tag name (e.g. <c>latest</c>).
/// </summary>
public sealed class ListRevisionTagsEscEnvironmentsVersions2Endpoint(IEnvironmentStore environments)
    : Endpoint<ListRevisionTagsEscEnvironmentsVersions2Request, ListEnvironmentRevisionTagsResponse>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/versions/{version}/tags");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ListRevisionTags")
            .WithName("ListRevisionTagsEscEnvironmentsVersions2")
        );
    }

    public override async Task HandleAsync(ListRevisionTagsEscEnvironmentsVersions2Request req, CancellationToken ct)
    {
        var revisions = environments.ListRevisions(new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName));
        var revision = ResolveRevision(revisions, req.Version);

        var tags = (revision?.Tags ?? new List<string>())
            .Select(name => EscTagMapper.RevisionTag(revision!, name)).ToList();
        await Send.OkAsync(new ListEnvironmentRevisionTagsResponse { Tags = tags, NextToken = "" }, ct);
    }

    // A version is either a revision number or an existing tag name pointing at one.
    private static StoredEnvRevision? ResolveRevision(IReadOnlyList<StoredEnvRevision> revisions, string version)
        => long.TryParse(version, out var number)
            ? revisions.FirstOrDefault(r => r.Number == number)
            : revisions.FirstOrDefault(r => r.Tags.Contains(version));
}
