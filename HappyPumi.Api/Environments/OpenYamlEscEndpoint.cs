// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// OpenYAML — fully evaluates a raw YAML definition posted in the body (no stored environment required) and
/// returns a session id. Imports in the definition are still resolved against the org's stored environments.
/// </summary>
public sealed class OpenYamlEscEndpoint(EscOpener opener, IEscSessionStore sessions)
    : Endpoint<OpenYamlEscRequest, OpenEnvironmentResponse>
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(2);

    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/yaml/open");
        Permissions("environment:read");
        Description(b => b
            .Accepts<OpenYamlEscRequest>("application/x-yaml", "text/yaml", "text/plain", "application/json")
            .WithTags("Environments")
            .WithSummary("OpenYAML")
            .WithName("OpenYamlEsc")
        );
    }

    public override async Task HandleAsync(OpenYamlEscRequest req, CancellationToken ct)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var yaml = await reader.ReadToEndAsync(ct);

        // Anonymous definition: no project/name, but imports resolve within the org.
        var coords = new EnvCoordinates(req.OrgName, "", "");
        var properties = await opener.OpenAsync(coords, yaml, ct);
        var id = sessions.Create(properties, GoDuration.Parse(req.Duration, DefaultDuration));
        await Send.OkAsync(new OpenEnvironmentResponse { Id = id, Diagnostics = new List<EnvironmentDiagnostic>() }, ct);
    }
}
