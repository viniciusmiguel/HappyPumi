// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// DecryptEnvironment — returns the definition YAML with static <c>fn::secret</c> values in plaintext (it does
/// not run <c>fn::open</c> providers). HappyPumi stores definitions unencrypted at rest (the at-rest crypter,
/// ADR-0007, covers stack checkpoints, not ESC definitions), so this returns the stored YAML as-is; the
/// endpoint exists for wire-compatibility and to gate plaintext access behind the read permission.
/// </summary>
public sealed class DecryptEnvironmentEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<DecryptEnvironmentEscEnvironmentsRequest, string>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/decrypt");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("DecryptEnvironment")
            .WithName("DecryptEnvironmentEscEnvironments")
        );
    }

    public override async Task HandleAsync(DecryptEnvironmentEscEnvironmentsRequest req, CancellationToken ct)
    {
        var env = environments.Get(new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName));
        if (env is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        EscHeaders.SetRevision(HttpContext, env.CurrentRevision);
        await Send.StringAsync(env.Yaml, contentType: "application/x-yaml; charset=utf-8", cancellation: ct);
    }
}
