// Implemented by hand (ESC draft-preview alias). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// ReadEnvironmentDraft (preview alias) — the <c>/api/preview/esc/</c> twin of
/// <see cref="ReadEnvironmentDraftEndpoint"/>: returns the draft's YAML definition (404 when unknown).
/// </summary>
public sealed class ReadEnvironmentDraftPreviewEndpoint(IEscDraftStore drafts)
    : Endpoint<ReadEnvironmentDraftPreviewRequest, string>
{
    public override void Configure()
    {
        Get("/api/preview/esc/environments/{orgName}/{projectName}/{envName}/drafts/{changeRequestID}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ReadEnvironmentDraft")
            .WithName("ReadEnvironmentDraftPreview")
        );
    }

    public override async Task HandleAsync(ReadEnvironmentDraftPreviewRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var draft = drafts.Get(coords, req.ChangeRequestId);
        if (draft is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.StringAsync(draft.Yaml, contentType: "application/x-yaml; charset=utf-8", cancellation: ct);
    }
}
