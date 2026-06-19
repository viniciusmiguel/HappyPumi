// Implemented by hand (ESC tags). The generator would overwrite this body; preserve it.
#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// ListAllEnvironmentTags — a map of every tag name to its distinct values across all environments in the
/// org (for tag-based filtering / discovery UIs).
/// </summary>
public sealed class ListAllEnvironmentTagsEscEndpoint(IEnvironmentStore environments)
    : Endpoint<ListAllEnvironmentTagsEscRequest, Dictionary<string, List<string>>>
{
    public override void Configure()
    {
        Get("/api/esc/environments/{orgName}/tags");
        Permissions("environment:read");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("ListAllEnvironmentTags")
            .WithName("ListAllEnvironmentTagsEsc")
        );
    }

    public override async Task HandleAsync(ListAllEnvironmentTagsEscRequest req, CancellationToken ct)
    {
        var byName = new Dictionary<string, SortedSet<string>>();
        foreach (var env in environments.ListByOrg(req.OrgName))
            foreach (var (name, value) in env.Tags)
                (byName.TryGetValue(name, out var set) ? set : byName[name] = new SortedSet<string>()).Add(value);

        var result = new Dictionary<string, List<string>>();
        foreach (var (name, values) in byName)
            result[name] = new List<string>(values);
        await Send.OkAsync(result, ct);
    }
}
