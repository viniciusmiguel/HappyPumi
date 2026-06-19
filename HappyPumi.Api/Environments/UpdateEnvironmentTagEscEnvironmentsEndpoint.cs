// Implemented by hand (ESC tags). The generator would overwrite this body; preserve it.
#nullable enable

using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// UpdateEnvironmentTag — renames and/or revalues an existing tag. The new name/value default to the current
/// ones when omitted; a rename removes the old key and writes the new one.
/// </summary>
public sealed class UpdateEnvironmentTagEscEnvironmentsEndpoint(IEnvironmentStore environments)
    : Endpoint<UpdateEnvironmentTagEscEnvironmentsRequest, EnvironmentTag>
{
    public override void Configure()
    {
        Patch("/api/esc/environments/{orgName}/{projectName}/{envName}/tags/{tagName}");
        Permissions("environment:write");
        Description(b => b
            .WithTags("Environments")
            .WithSummary("UpdateEnvironmentTag")
            .WithName("UpdateEnvironmentTagEscEnvironments")
        );
    }

    public override async Task HandleAsync(UpdateEnvironmentTagEscEnvironmentsRequest req, CancellationToken ct)
    {
        var coords = new EnvCoordinates(req.OrgName, req.ProjectName, req.EnvName);
        var env = environments.Get(coords);
        if (env is null || !env.Tags.TryGetValue(req.TagName, out var currentValue))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var newName = req.Body?.NewTag?.Name is { Length: > 0 } n ? n : req.TagName;
        var newValue = req.Body?.NewTag?.Value ?? currentValue;
        if (newName != req.TagName)
            environments.DeleteTag(coords, req.TagName);
        var updated = environments.SetTag(coords, newName, newValue)!;
        await Send.OkAsync(EscTagMapper.From(updated, newName, newValue), ct);
    }
}
