// Implemented by hand (ESC draft-preview alias). The generator would overwrite this body; preserve it.
#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// CreateEnvironmentDraft (preview alias) — the <c>/api/preview/esc/</c> twin of
/// <see cref="CreateEnvironmentDraftEndpoint"/>: stores a proposed definition change (raw YAML body) as a draft
/// and registers the wrapping change request keyed by the draft id.
/// </summary>
public sealed class CreateEnvironmentDraftPreviewEndpoint(
    IEnvironmentStore environments, IEscDraftStore drafts, IChangeRequestStore changeRequests)
    : Endpoint<CreateEnvironmentDraftPreviewRequest, ChangeRequestRef>
{
    public override void Configure()
    {
        Post("/api/preview/esc/environments/{orgName}/{projectName}/{envName}/drafts");
        Permissions("environment:write");
        Description(b => b
            .Accepts<CreateEnvironmentDraftPreviewRequest>("application/x-yaml", "text/yaml", "text/plain", "application/json")
            .WithTags("Environments")
            .WithSummary("CreateEnvironmentDraft")
            .WithName("CreateEnvironmentDraftPreview")
        );
    }

    public override async Task HandleAsync(CreateEnvironmentDraftPreviewRequest req, CancellationToken ct)
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
        // The change-request id is the draft id: register a CR record wrapping this env draft (PR2).
        changeRequests.Create(new StoredChangeRequest
        {
            Id = id,
            Org = req.OrgName,
            Action = "update",
            Description = "",
            TargetProject = req.ProjectName,
            TargetEnv = req.EnvName,
            Status = "draft",
            LatestRevisionNumber = env.CurrentRevision,
            CreatedBy = User.Identity?.Name ?? "happypumi",
            CreatedAt = DateTime.UtcNow,
        });
        await Send.OkAsync(new ChangeRequestRef { ChangeRequestId = id, LatestRevisionNumber = env.CurrentRevision }, ct);
    }
}
