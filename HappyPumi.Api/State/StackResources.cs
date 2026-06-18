#nullable enable

using System.Collections.Generic;
using System.Text.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Reads the resource list out of a stack's latest state checkpoint. The checkpoint is the raw Pulumi
/// deployment (apitype) stored untyped; its <c>resources</c> array is the engine's resource registry.
/// </summary>
public static class StackResources
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Extracts the managed resources from a stack checkpoint, or an empty list when there is none.</summary>
    public static List<Resource> Extract(AppUntypedDeployment? deployment)
    {
        var result = new List<Resource>();
        if (deployment?.Deployment is null)
            return result;

        // Deployment is stored untyped (object/JsonElement); round-trip to JSON to read the resources array.
        var json = JsonSerializer.Serialize(deployment.Deployment, Options);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("resources", out var resources) || resources.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var element in resources.EnumerateArray())
        {
            var resource = element.Deserialize<Resource>(Options);
            if (resource is not null)
                result.Add(resource);
        }
        return result;
    }
}
