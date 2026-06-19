// Implemented by hand (ESC open lifecycle). The generator would overwrite this body; preserve it.
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

/// <summary>
/// OpenEnvironment — fully evaluates an environment (imports, interpolation, built-ins, <c>fn::open</c>
/// providers, secret decryption) and stores the resolved tree under a short-lived session id.
/// </summary>
public sealed class OpenEnvironmentEscEnvironmentsEndpoint(IEnvironmentStore environments, EscOpener opener, IEscSessionStore sessions)
    : Endpoint<OpenEnvironmentEscEnvironmentsRequest, OpenEnvironmentResponse>
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(2);

    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/open");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("OpenEnvironment")
            .WithName("OpenEnvironmentEscEnvironments")
        );
    }

    public override async Task HandleAsync(OpenEnvironmentEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var env = environments.Get(coords);
        if (env is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var properties = await opener.OpenAsync(coords, env.Yaml, ct);
        var id = sessions.Create(properties, GoDuration.Parse(req.Duration, DefaultDuration));
        await Send.OkAsync(new OpenEnvironmentResponse { Id = id, Diagnostics = new List<EnvironmentDiagnostic>() }, ct);
    }
}
