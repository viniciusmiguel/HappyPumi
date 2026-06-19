// Implemented by hand (ESC versions). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ReadEnvironment (by version) — the YAML definition of a specific revision (number or tag name).</summary>
public sealed class ReadEnvironmentEscEnvironmentsVersionsEndpoint(IEnvironmentStore environments)
    : Endpoint<ReadEnvironmentEscEnvironmentsVersionsRequest, string>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/versions/{version}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ReadEnvironment")
            .WithName("ReadEnvironmentEscEnvironmentsVersions")
        );
    }

    public override async Task HandleAsync(ReadEnvironmentEscEnvironmentsVersionsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var revision = EscVersionResolver.Resolve(environments.ListRevisions(coords), req.Version);
        if (revision is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.StringAsync(revision.Yaml, contentType: "application/x-yaml; charset=utf-8", cancellation: ct);
    }
}
