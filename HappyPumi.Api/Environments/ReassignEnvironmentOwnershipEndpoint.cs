// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// ReassignEnvironmentOwnership — sets a new owner for the environment and returns the previous owner's
/// identity (per the ESC contract).
/// </summary>
public sealed class ReassignEnvironmentOwnershipEndpoint(IEnvironmentStore environments)
    : Endpoint<ReassignEnvironmentOwnershipRequest, UserInfo>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/ownership");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ReassignEnvironmentOwnership")
            .WithName("ReassignEnvironmentOwnership")
        );
    }

    public override async Task HandleAsync(ReassignEnvironmentOwnershipRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var env = environments.Get(coords);
        if (env is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var previousOwner = new UserInfo { Name = env.OwnerName, GithubLogin = env.OwnerLogin };
        var newLogin = !string.IsNullOrWhiteSpace(req.Body?.GithubLogin) ? req.Body.GithubLogin : req.Body?.Name ?? "";
        environments.ReassignOwner(coords, newLogin, req.Body?.Name ?? newLogin);
        await Send.OkAsync(previousOwner, ct);
    }
}
