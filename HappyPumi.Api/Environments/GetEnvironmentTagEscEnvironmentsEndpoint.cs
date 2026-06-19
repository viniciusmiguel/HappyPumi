// Implemented by hand (ESC tags). The generator would overwrite this body; preserve it.
#nullable enable

using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>GetEnvironmentTag — returns a single environment tag, or 404 when it is not set.</summary>
public sealed class GetEnvironmentTagEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<GetEnvironmentTagEscEnvironmentsRequest, EnvironmentTag>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/tags/{tagName}");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("GetEnvironmentTag")
            .WithName("GetEnvironmentTagEscEnvironments")
        );
    }

    public override async Task HandleAsync(GetEnvironmentTagEscEnvironmentsRequest req, CancellationToken ct)
    {
        var env = environments.Get(new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName));
        if (env is null || !env.Tags.TryGetValue(req.TagName, out var value))
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(EscTagMapper.From(env, req.TagName, value), ct);
    }
}
