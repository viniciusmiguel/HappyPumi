// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Request for CloneEnvironment with the correct body type (the generator self-referenced the request type).
/// </summary>
public sealed class CloneEnvironmentInput
{
    [BindFrom("orgName")] public string OrgName { get; set; } = default!;
    [BindFrom("projectName")] public string ProjectName { get; set; } = default!;
    [BindFrom("envName")] public string EnvName { get; set; } = default!;
    // Fully qualified: the same-namespace generated request of the same name would otherwise shadow this.
    [FromBody] public HappyPumi.Api.Contracts.CloneEnvironmentRequest Settings { get; set; } = default!;
}

/// <summary>
/// CloneEnvironment — duplicates an environment's current definition into a new project/name (the mechanism
/// for renaming, which ESC does not support directly). <c>preserveEnvironmentTags</c> copies the tags; full
/// revision-history preservation (<c>preserveHistory</c>) is not yet implemented — the clone starts fresh.
/// </summary>
public sealed class CloneEnvironmentEndpoint(IEnvironmentStore environments) : Endpoint<CloneEnvironmentInput>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/clone");
        Permissions("environment:create");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("CloneEnvironment")
            .WithName("CloneEnvironment")
        );
    }

    public override async Task HandleAsync(CloneEnvironmentInput req, CancellationToken ct)
    {
        var source = environments.Get(new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName));
        if (source is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        if (string.IsNullOrWhiteSpace(req.Settings?.Name))
        {
            AddError("'name' is required to clone an environment.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var login = User.Identity?.Name ?? "happypumi";
        var target = new EnvCoordinates(req.OrgName, req.Settings.Project ?? req.ProjectName, req.Settings.Name);
        if (environments.Create(target, login, login) is null)
        {
            await Send.ResultAsync(Results.Conflict(
                new { message = $"Environment '{target.Project}/{target.Name}' already exists." }));
            return;
        }

        environments.UpdateYaml(target, source.Yaml, login, login);
        if (req.Settings.PreserveEnvironmentTags == true)
            foreach (var (name, value) in source.Tags)
                environments.SetTag(target, name, value);

        await Send.ResultAsync(Results.Created(
            $"/api/esc/environments/{req.OrgName}/{target.Project}/{target.Name}", (object?)null));
    }
}
