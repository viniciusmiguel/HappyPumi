// Implemented by hand (ESC settings). The generator would overwrite this body; preserve it.
#nullable enable

using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Request for PatchEnvironmentSettings with the correct body type. The generator bound the body to the
/// request type itself (a self-reference), so this hand-written DTO supplies the real <c>deletionProtected</c>
/// payload (an <see cref="EnvironmentSettings"/>) while still binding the route parameters.
/// </summary>
public sealed class PatchEnvironmentSettingsInput
{
    [BindFrom("orgName")] public string OrgName { get; set; } = default!;
    [BindFrom("projectName")] public string ProjectName { get; set; } = default!;
    [BindFrom("envName")] public string EnvName { get; set; } = default!;
    [FromBody] public EnvironmentSettings Settings { get; set; } = default!;
}

/// <summary>PatchEnvironmentSettings — toggles deletion protection on an environment.</summary>
public sealed class PatchEnvironmentSettingsEndpoint(IEnvironmentStore environments)
    : Endpoint<PatchEnvironmentSettingsInput>
{
    public override void Configure()
    {
        Patch("/api/esc/environments/{orgName}/{projectName}/{envName}/settings");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("PatchEnvironmentSettings")
            .WithName("PatchEnvironmentSettings")
        );
    }

    public override async Task HandleAsync(PatchEnvironmentSettingsInput req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var updated = environments.SetDeletionProtected(coords, req.Settings.DeletionProtected);
        if (updated is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
