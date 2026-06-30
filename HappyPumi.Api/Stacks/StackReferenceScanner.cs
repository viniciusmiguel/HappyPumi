#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Stacks;

/// <summary>
/// Derives stack-to-stack references from checkpoint <c>pulumi:pulumi:StackReference</c> resources.
/// Upstream = the stacks a given stack reads (its own StackReference resources); downstream = the stacks
/// that read a given stack (other stacks whose StackReference points back at it). No dedicated store — the
/// relationships live in the resource graph.
/// </summary>
internal static class StackReferenceScanner
{
    /// <summary>The fully-qualified stack names ("org/project/stack") this stack reads via StackReference.</summary>
    public static IEnumerable<StackCoordinates> UpstreamOf(StoredStack stack)
        => ReferenceTargets(stack);

    /// <summary>The stacks (other than the target) whose StackReference resources point at the target.</summary>
    public static IEnumerable<StackCoordinates> DownstreamOf(StackCoordinates target, IEnumerable<StoredStack> all)
        => all.Where(s => s.Coordinates != target && ReferenceTargets(s).Contains(target))
              .Select(s => s.Coordinates);

    /// <summary>Maps coordinates to the API <see cref="StackReference"/>, resolving the live version when known.</summary>
    public static StackReference ToReference(StackCoordinates c, IStackStore stacks) => new()
    {
        Name = c.Stack,
        Organization = c.Org,
        RoutingProject = c.Project,
        Version = stacks.Find(c)?.Version ?? 0,
    };

    private static IEnumerable<StackCoordinates> ReferenceTargets(StoredStack stack)
    {
        foreach (var resource in StackResources.Extract(stack.Deployment))
        {
            if (resource.Type != "pulumi:pulumi:StackReference")
                continue;
            var name = ReferenceName(resource);
            if (name is not null && TryParse(name, out var coords))
                yield return coords;
        }
    }

    // The target stack ref is the StackReference resource's "name" input ("org/project/stack").
    private static string? ReferenceName(Resource resource)
    {
        if (resource.Inputs is null || !resource.Inputs.TryGetValue("name", out var value) || value is null)
            return null;
        return value is JsonElement { ValueKind: JsonValueKind.String } el ? el.GetString() : value.ToString();
    }

    private static bool TryParse(string qualified, out StackCoordinates coords)
    {
        var parts = qualified.Split('/');
        if (parts.Length == 3)
        {
            coords = new StackCoordinates(parts[0], parts[1], parts[2]);
            return true;
        }
        coords = default;
        return false;
    }
}
