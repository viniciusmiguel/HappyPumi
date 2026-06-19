// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Request for UpdateEnvironment. Implements <see cref="IPlainTextRequest"/> so the YAML body is read raw into
/// <see cref="Content"/> instead of being JSON-deserialized — the CLI PATCHes the definition with an
/// application/json content type, which would otherwise make FastEndpoints try to parse the YAML as JSON.
/// </summary>
public sealed class UpdateEnvironmentInput : IPlainTextRequest
{
    [BindFrom("orgName")] public string OrgName { get; set; } = default!;
    [BindFrom("projectName")] public string ProjectName { get; set; } = default!;
    [BindFrom("envName")] public string EnvName { get; set; } = default!;
    public string Content { get; set; } = default!;
}

/// <summary>
/// UpdateEnvironment — replaces the definition YAML (recording a new revision). The body is raw YAML; an
/// unparseable definition is rejected with an error diagnostic rather than persisted.
/// </summary>
public sealed class UpdateEnvironmentEscEnvironmentsEndpoint(IEnvironmentStore environments, IAuditLog audit)
    : Endpoint<UpdateEnvironmentInput, UpdateEnvironmentResponse>
{
    public override void Configure()
    {
        Patch("/api/esc/environments/{orgName}/{projectName}/{envName}");
        Permissions("environment:write");
        Description(b => b
            .Accepts<UpdateEnvironmentInput>("application/x-yaml", "text/yaml", "text/plain", "application/json")
            .WithTags("Environments")
            .WithSummary("UpdateEnvironment")
            .WithName("UpdateEnvironmentEscEnvironments")
        );
    }

    public override async Task HandleAsync(UpdateEnvironmentInput req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        if (environments.Get(coords) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var yaml = req.Content ?? "";
        var diagnostic = ValidationError(yaml);
        if (diagnostic is not null)
        {
            await Send.OkAsync(new UpdateEnvironmentResponse { Diagnostics = new List<EnvironmentDiagnostic> { diagnostic } }, ct);
            return;
        }

        var login = User.Identity?.Name ?? "happypumi";
        var updated = environments.UpdateYaml(coords, yaml, login, login);
        audit.Record(req.OrgName, "environment.update",
            $"Updated environment '{req.ProjectName}/{req.EnvName}' (revision {updated?.CurrentRevision ?? 0})", login);
        EscHeaders.SetRevision(HttpContext, updated?.CurrentRevision ?? 0); // the CLI parses the new revision from the response
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
