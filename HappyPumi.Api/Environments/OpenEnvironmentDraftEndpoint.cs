// Implemented by hand (ESC drafts). The generator would overwrite this body; preserve it.
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>OpenEnvironmentDraft — fully evaluates a draft's proposed definition and returns a session id.</summary>
public sealed class OpenEnvironmentDraftEndpoint(IEscDraftStore drafts, EscOpener opener, IEscSessionStore sessions)
    : Endpoint<OpenEnvironmentDraftRequest, OpenEnvironmentResponse>
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(2);

    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/drafts/{changeRequestID}/open");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("OpenEnvironmentDraft")
            .WithName("OpenEnvironmentDraft")
        );
    }

    public override async Task HandleAsync(OpenEnvironmentDraftRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var draft = drafts.Get(coords, req.ChangeRequestId);
        if (draft is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var properties = await opener.OpenAsync(coords, draft.Yaml, ct);
        var id = sessions.Create(properties, GoDuration.Parse(req.Duration, DefaultDuration));
        await Send.OkAsync(new OpenEnvironmentResponse { Id = id, Diagnostics = new List<EnvironmentDiagnostic>() }, ct);
    }
}
