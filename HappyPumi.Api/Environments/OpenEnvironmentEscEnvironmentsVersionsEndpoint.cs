// Implemented by hand (ESC versions). The generator would overwrite this body; preserve it.
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

/// <summary>OpenEnvironment (by version) — fully evaluates a specific revision and returns a session id.</summary>
public sealed class OpenEnvironmentEscEnvironmentsVersionsEndpoint(IEnvironmentStore environments, EscOpener opener, IEscSessionStore sessions, EscOpenGate gate)
    : Endpoint<OpenEnvironmentEscEnvironmentsVersionsRequest, OpenEnvironmentResponse>
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(2);

    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/versions/{version}/open");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("OpenEnvironment")
            .WithName("OpenEnvironmentEscEnvironmentsVersions")
        );
    }

    public override async Task HandleAsync(OpenEnvironmentEscEnvironmentsVersionsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var revision = EscVersionResolver.Resolve(environments.ListRevisions(coords), req.Version);
        if (revision is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var requester = User.Identity?.Name ?? "happypumi";
        if (!gate.Allows(coords, requester, DateTime.UtcNow))
        {
            await Send.ResultAsync(Microsoft.AspNetCore.Http.Results.Json(
                new { code = 403, message = $"Opening '{req.ProjectName}/{req.EnvName}' requires an approved access request." },
                statusCode: 403));
            return;
        }

        var properties = await opener.OpenAsync(coords, revision.Yaml, ct);
        var id = sessions.Create(properties, GoDuration.Parse(req.Duration, DefaultDuration));
        await Send.OkAsync(new OpenEnvironmentResponse { Id = id, Diagnostics = new List<EnvironmentDiagnostic>() }, ct);
    }
}
