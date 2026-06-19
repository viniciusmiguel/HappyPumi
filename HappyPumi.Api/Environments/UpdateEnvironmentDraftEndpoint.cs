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

/// <summary>UpdateEnvironmentDraft — replaces a draft's YAML (404 when the change request is unknown).</summary>
public sealed class UpdateEnvironmentDraftEndpoint(IEscDraftStore drafts)
    : Endpoint<UpdateEnvironmentDraftRequest, ChangeRequestRef>
{
    public override void Configure()
    {
        Patch("/api/esc/environments/{orgName}/{projectName}/{envName}/drafts/{changeRequestID}");
        Permissions("environment:write");
        Description(b => b
            .Accepts<UpdateEnvironmentDraftRequest>("application/x-yaml", "text/yaml", "text/plain", "application/json")
            .WithTags("Environments")
            .WithSummary("UpdateEnvironmentDraft")
            .WithName("UpdateEnvironmentDraft")
        );
    }

    public override async Task HandleAsync(UpdateEnvironmentDraftRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        using var reader = new StreamReader(HttpContext.Request.Body);
        var yaml = await reader.ReadToEndAsync(ct);
        if (!drafts.Update(coords, req.ChangeRequestId, yaml))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        var draft = drafts.Get(coords, req.ChangeRequestId)!;
        await Send.OkAsync(new ChangeRequestRef { ChangeRequestId = draft.Id, LatestRevisionNumber = draft.BaseRevision }, ct);
    }
}
