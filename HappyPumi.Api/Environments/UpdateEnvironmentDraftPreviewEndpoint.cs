// Implemented by hand (ESC draft-preview alias). The generator would overwrite this body; preserve it.
#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Organizations;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// UpdateEnvironmentDraft (preview alias) — the <c>/api/preview/esc/</c> twin of
/// <see cref="UpdateEnvironmentDraftEndpoint"/>: replaces a draft's YAML (404 when unknown) and clears the
/// change request's approvals when an applicable gate requires reapproval on change (PR3).
/// </summary>
public sealed class UpdateEnvironmentDraftPreviewEndpoint(
    IEscDraftStore drafts, IChangeRequestStore changeRequests, ChangeGateEvaluator evaluator)
    : Endpoint<UpdateEnvironmentDraftPreviewRequest, ChangeRequestRef>
{
    public override void Configure()
    {
        Patch("/api/preview/esc/environments/{orgName}/{projectName}/{envName}/drafts/{changeRequestID}");
        Permissions("environment:write");
        Description(b => b
            .Accepts<UpdateEnvironmentDraftPreviewRequest>("application/x-yaml", "text/yaml", "text/plain", "application/json")
            .WithTags("Environments")
            .WithSummary("UpdateEnvironmentDraft")
            .WithName("UpdateEnvironmentDraftPreview")
        );
    }

    public override async Task HandleAsync(UpdateEnvironmentDraftPreviewRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        using var reader = new StreamReader(HttpContext.Request.Body);
        var yaml = await reader.ReadToEndAsync(ct);
        if (!drafts.Update(coords, req.ChangeRequestId, yaml))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        ChangeRequestReapproval.ClearIfRequired(changeRequests, evaluator, req.OrgName, req.ChangeRequestId);
        var draft = drafts.Get(coords, req.ChangeRequestId)!;
        await Send.OkAsync(new ChangeRequestRef { ChangeRequestId = draft.Id, LatestRevisionNumber = draft.BaseRevision }, ct);
    }
}
