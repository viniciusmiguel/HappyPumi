// Implemented by hand (ESC drafts). The generator would overwrite this body; preserve it.
#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>CreateEnvironmentDraft — stores a proposed definition change (raw YAML body) as a change request.</summary>
public sealed class CreateEnvironmentDraftEndpoint(IEnvironmentStore environments, IEscDraftStore drafts)
    : Endpoint<CreateEnvironmentDraftRequest, ChangeRequestRef>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/drafts");
        Permissions("environment:write");
        Description(b => b
            .Accepts<CreateEnvironmentDraftRequest>("application/x-yaml", "text/yaml", "text/plain", "application/json")
            .WithTags("Environments")
            .WithSummary("CreateEnvironmentDraft")
            .WithName("CreateEnvironmentDraft")
        );
    }

    public override async Task HandleAsync(CreateEnvironmentDraftRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var env = environments.Get(coords);
        if (env is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        using var reader = new StreamReader(HttpContext.Request.Body);
        var yaml = await reader.ReadToEndAsync(ct);
        var id = drafts.Create(coords, yaml, env.CurrentRevision);
        await Send.OkAsync(new ChangeRequestRef { ChangeRequestId = id, LatestRevisionNumber = env.CurrentRevision }, ct);
    }
}
