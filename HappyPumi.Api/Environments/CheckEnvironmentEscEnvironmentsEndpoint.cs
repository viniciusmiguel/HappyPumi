// Implemented by hand (ESC lifecycle). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// CheckEnvironment — validates a stored environment's definition and returns its evaluated property tree
/// (without executing <c>fn::open</c> providers). Secrets are masked unless <c>showSecrets=true</c>.
/// </summary>
public sealed class CheckEnvironmentEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<CheckEnvironmentEscEnvironmentsRequest, CheckEnvironmentResponse>
{
    public override void Configure()
    {
        Post("/api/esc/environments/{orgName}/{projectName}/{envName}/check");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("CheckEnvironment")
            .WithName("CheckEnvironmentEscEnvironments")
        );
    }

    public override async Task HandleAsync(CheckEnvironmentEscEnvironmentsRequest req, CancellationToken ct)
    {
        var env = environments.Get(new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName));
        if (env is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var properties = EnvironmentEvaluator.Evaluate(env.Yaml);
        if (req.ShowSecrets != true)
            EscRedactor.Mask(properties);

        await Send.OkAsync(new CheckEnvironmentResponse
        {
            Properties = properties,
            Diagnostics = new List<EnvironmentDiagnostic>(),
        }, ct);
    }
}
