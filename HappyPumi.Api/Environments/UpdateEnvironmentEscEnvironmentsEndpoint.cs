// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using HappyPumi.Api.Webhooks;

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
public sealed class UpdateEnvironmentEscEnvironmentsEndpoint(
    IEnvironmentStore environments, IAuditLog audit, IEnvironmentWebhookStore webhooks, IWebhookDispatcher dispatcher)
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
        await FireUpdatedAsync(coords, updated?.CurrentRevision ?? 0, ct);
        EscHeaders.SetRevision(HttpContext, updated?.CurrentRevision ?? 0); // the CLI parses the new revision from the response
        await Send.OkAsync(new UpdateEnvironmentResponse { Diagnostics = new List<EnvironmentDiagnostic>() }, ct);
    }

    // Best-effort: notify the env's webhooks of the new revision. The dispatcher records (never throws) on
    // delivery failure; we also swallow any resolution error so a webhook can never fault the update itself.
    private async Task FireUpdatedAsync(EnvCoordinates coords, long revision, CancellationToken ct)
    {
        try
        {
            var hooks = webhooks.List(coords).Select(w => WebhookMapper.ToSigningTarget(w, coords));
            var payload = new { kind = "env_updated", environment = $"{coords.Org}/{coords.Project}/{coords.Name}", revision };
            await dispatcher.FireAsync(EnvWebhookScope.For(coords), hooks, "env_updated", payload, ct);
        }
        catch
        {
            // firing is observability, not part of the update contract — never surface its failure
        }
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
