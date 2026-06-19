// Implemented by hand (ESC rotation). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// RotateEnvironment — executes every <c>fn::rotate</c> declaration, persists the rotated state as a new
/// revision, and returns the rotation event.
/// </summary>
public sealed class RotateEnvironmentEndpoint(EscRotationRunner runner, IAuditLog audit)
    : Endpoint<RotateEnvironmentRequest, RotateEnvironmentResponse>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/rotate");
        Permissions("environment:write");
        Description(b => b
            .Accepts<RotateEnvironmentRequest>("application/json")
            .WithTags("Environments")
            .WithSummary("RotateEnvironment")
            .WithName("RotateEnvironment")
        );
    }

    public override async Task HandleAsync(RotateEnvironmentRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var login = User.Identity?.Name ?? "happypumi";
        var rotationEvent = await runner.RotateAsync(coords, login, ct);
        if (rotationEvent is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        audit.Record(req.OrgName, "environment.rotate",
            $"Rotated secrets in environment '{req.ProjectName}/{req.EnvName}'", login);
        await Send.OkAsync(new RotateEnvironmentResponse
        {
            Id = rotationEvent.Id,
            SecretRotationEvent = rotationEvent,
            Diagnostics = new List<EnvironmentDiagnostic>(),
        }, ct);
    }
}
