// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using FastEndpoints;
using HappyPumi.Api.State;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Request for RestoreEnvironment. The generator self-referenced the request type for the body and its
/// contract lacks the target identity, so this hand-written body carries the project + name to restore.
/// </summary>
public sealed class RestoreEnvironmentInput
{
    [BindFrom("orgName")] public string OrgName { get; set; } = default!;
    [FromBody] public RestoreEnvironmentBody Body { get; set; } = default!;
}

/// <summary>Identifies the soft-deleted environment to restore.</summary>
public sealed class RestoreEnvironmentBody
{
    public string Project { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? DeletionTimestamp { get; set; }
}

/// <summary>RestoreEnvironment — restores a soft-deleted environment within the retention window.</summary>
public sealed class RestoreEnvironmentEndpoint(IEnvironmentStore environments) : Endpoint<RestoreEnvironmentInput>
{
    public override void Configure()
    {
        Put("/api/esc/environments/{orgName}/restore");
        Permissions("environment:create");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("RestoreEnvironment")
            .WithName("RestoreEnvironment")
        );
    }

    public override async Task HandleAsync(RestoreEnvironmentInput req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Body?.Project) || string.IsNullOrWhiteSpace(req.Body.Name))
        {
            AddError("'project' and 'name' are required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var restored = environments.Restore(new EnvCoordinates(req.OrgName, req.Body.Project, req.Body.Name));
        if (restored is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.NoContentAsync(ct);
    }
}
