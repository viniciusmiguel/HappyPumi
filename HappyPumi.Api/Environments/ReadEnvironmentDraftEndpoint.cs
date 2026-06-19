// Implemented by hand (ESC drafts). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ReadEnvironmentDraft — the draft's YAML definition (404 when the change request is unknown).</summary>
public sealed class ReadEnvironmentDraftEndpoint(IEscDraftStore drafts)
    : Endpoint<ReadEnvironmentDraftRequest, string>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/drafts/{changeRequestID}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ReadEnvironmentDraft")
            .WithName("ReadEnvironmentDraft")
        );
    }

    public override async Task HandleAsync(ReadEnvironmentDraftRequest req, CancellationToken ct)
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
