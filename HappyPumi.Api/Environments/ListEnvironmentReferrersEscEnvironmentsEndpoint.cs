// Implemented by hand (ESC referrers). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// ListEnvironmentReferrers — the environments in the org whose <c>imports</c> reference this one (the inverse
/// of imports). Stack referrers are not tracked yet, so only environment referrers are returned, keyed by the
/// importing environment's <c>project/name</c>.
/// </summary>
public sealed class ListEnvironmentReferrersEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<ListEnvironmentReferrersEscEnvironmentsRequest, ListEnvironmentReferrersResponse>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/{projectName}/{envName}/referrers");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ListEnvironmentReferrers")
            .WithName("ListEnvironmentReferrersEscEnvironments")
        );
    }

    public override async Task HandleAsync(ListEnvironmentReferrersEscEnvironmentsRequest req, CancellationToken ct)
    {
        var target = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var referrers = new Dictionary<string, List<EnvironmentReferrer>>();

        foreach (var env in environments.ListByOrg(req.OrgName))
        {
            if (env.Coordinates.Equals(target) || !ImportsTarget(env, target))
                continue;
            referrers[$"{env.Coordinates.Project}/{env.Coordinates.Name}"] = new List<EnvironmentReferrer>
            {
                new()
                {
                    Environment = new EnvironmentImportReferrer
                    {
                        Project = env.Coordinates.Project, Name = env.Coordinates.Name, Revision = env.CurrentRevision,
                    },
                },
            };
        }

        await Send.OkAsync(new ListEnvironmentReferrersResponse { Referrers = referrers, ContinuationToken = "" }, ct);
    }

    // True when any of the candidate's imports resolves to the target environment (default project = importer's).
    private static bool ImportsTarget(StoredEnvironment candidate, EnvCoordinates target)
    {
        var root = EnvironmentEvaluator.ParseRoot(candidate.Yaml);
        if (root.GetValueOrDefault("imports") is not List<object?> imports)
            return false;
        return imports.OfType<string>().Any(import =>
        {
            var parts = import.Split('@', 2)[0].Split('/', 2);
            var (project, name) = parts.Length == 2
                ? (parts[0], parts[1])
                : (candidate.Coordinates.Project, parts[0]);
            return project == target.Project && name == target.Name;
        });
    }
}
