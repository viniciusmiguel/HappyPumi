// Implemented by hand (ESC tags). The generator would overwrite this body; preserve it.
#nullable enable

using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>CreateEnvironmentTag — adds a new tag (409 if one with the same name already exists).</summary>
public sealed class CreateEnvironmentTagEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<CreateEnvironmentTagEscEnvironmentsRequest, EnvironmentTag>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/tags");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("CreateEnvironmentTag")
            .WithName("CreateEnvironmentTagEscEnvironments")
        );
    }

    public override async Task HandleAsync(CreateEnvironmentTagEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var env = environments.Get(coords);
        if (env is null || string.IsNullOrWhiteSpace(req.Body?.Name))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        if (env.Tags.ContainsKey(req.Body.Name))
        {
            await Send.ResultAsync(Results.Conflict(new { message = $"Tag '{req.Body.Name}' already exists." }));
            return;
        }

        var updated = environments.SetTag(coords, req.Body.Name, req.Body.Value)!;
        await Send.OkAsync(EscTagMapper.From(updated, req.Body.Name, req.Body.Value), ct);
    }
}
