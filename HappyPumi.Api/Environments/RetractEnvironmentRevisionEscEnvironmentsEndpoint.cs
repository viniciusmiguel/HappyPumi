// Implemented by hand (ESC revisions). The generator would overwrite this body; preserve it.
#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// RetractEnvironmentRevision — marks a revision withdrawn (it stays in history but is no longer a valid
/// version to use). The <c>version</c> path segment may be a revision number or an existing tag name.
/// </summary>
public sealed class RetractEnvironmentRevisionEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<RetractEnvironmentRevisionEscEnvironmentsRequest>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/versions/{version}/retract");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("RetractEnvironmentRevision")
            .WithName("RetractEnvironmentRevisionEscEnvironments")
        );
    }

    public override async Task HandleAsync(RetractEnvironmentRevisionEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var revisions = environments.ListRevisions(coords);
        var target = long.TryParse(req.Version, out var number)
            ? revisions.FirstOrDefault(r => r.Number == number)
            : revisions.FirstOrDefault(r => r.Tags.Contains(req.Version));
        if (target is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var login = User.Identity?.Name ?? "happypumi";
        environments.RetractRevision(coords, target.Number, req.Body?.Reason, req.Body?.Replacement, login, login);
        await Send.NoContentAsync(ct);
    }
}
