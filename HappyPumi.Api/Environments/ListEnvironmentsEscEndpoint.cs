// Implemented by hand (ESC environments list). The generator would overwrite this body; preserve it.
#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>ListEnvironments — every environment across all orgs (the `pulumi env ls` surface).</summary>
public sealed class ListEnvironmentsEscEndpoint(IEnvironmentStore environments)
    : Endpoint<ListEnvironmentsEscRequest, ListEnvironmentsResponse>
{
    public override void Configure()
    {
        Get("/api/esc/environments");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ListEnvironments")
            .WithName("ListEnvironmentsEsc")
        );
    }

    public override async Task HandleAsync(ListEnvironmentsEscRequest req, CancellationToken ct)
    {
        var envs = environments.ListAll().Select(EnvironmentMapper.ToOrgEnvironment).ToList();
        await Send.OkAsync(new ListEnvironmentsResponse { Environments = envs, NextToken = null }, ct);
    }
}
