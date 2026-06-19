// Implemented by hand (ESC versions). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>DecryptEnvironment (by version) — a specific revision's YAML with static secrets in plaintext.</summary>
public sealed class DecryptEnvironmentEscEnvironmentsVersionsEndpoint(IEnvironmentStore environments)
    : Endpoint<DecryptEnvironmentEscEnvironmentsVersionsRequest, string>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/versions/{version}/decrypt");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("DecryptEnvironment")
            .WithName("DecryptEnvironmentEscEnvironmentsVersions")
        );
    }

    public override async Task HandleAsync(DecryptEnvironmentEscEnvironmentsVersionsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var revision = EscVersionResolver.Resolve(environments.ListRevisions(coords), req.Version);
        if (revision is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        EscHeaders.SetRevision(HttpContext, revision.Number);
        await Send.StringAsync(revision.Yaml, contentType: "application/x-yaml; charset=utf-8", cancellation: ct);
    }
}
