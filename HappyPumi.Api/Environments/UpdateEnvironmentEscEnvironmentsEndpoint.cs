// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// UpdateEnvironment — replaces the definition YAML (recording a new revision). The body is raw YAML; an
/// unparseable definition is rejected with an error diagnostic rather than persisted.
/// </summary>
public sealed class UpdateEnvironmentEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<UpdateEnvironmentEscEnvironmentsRequest, UpdateEnvironmentResponse>
{
    public override void Configure()
    {
        Patch("/api/esc/environments/{orgName}/{projectName}/{envName}");
        Permissions("environment:write");
        // The definition is posted as raw YAML; read the body directly (see CheckYaml).
        Description(b => b
            .Accepts<UpdateEnvironmentEscEnvironmentsRequest>("application/x-yaml", "text/yaml", "text/plain", "application/json")
            .WithTags("Environments")
            .WithSummary("UpdateEnvironment")
            .WithName("UpdateEnvironmentEscEnvironments")
        );
    }

    public override async Task HandleAsync(UpdateEnvironmentEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (environments.Get(coords) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        using var reader = new StreamReader(HttpContext.Request.Body);
        var yaml = await reader.ReadToEndAsync(ct);

        var diagnostic = ValidationError(yaml);
        if (diagnostic is not null)
        {
            await Send.OkAsync(new UpdateEnvironmentResponse { Diagnostics = new List<EnvironmentDiagnostic> { diagnostic } }, ct);
            return;
        }

        var login = User.Identity?.Name ?? "happypumi";
        environments.UpdateYaml(coords, yaml, login, login);
        await Send.OkAsync(new UpdateEnvironmentResponse { Diagnostics = new List<EnvironmentDiagnostic>() }, ct);
    }

    // Reject a definition that does not parse; the offending parser message is surfaced to the editor.
    private static EnvironmentDiagnostic? ValidationError(string yaml)
    {
        try
        {
            EnvironmentEvaluator.ParseRoot(yaml);
            return null;
        }
        catch (Exception ex)
        {
            return new EnvironmentDiagnostic { Severity = "error", Summary = $"Invalid environment definition: {ex.Message}" };
        }
    }
}
