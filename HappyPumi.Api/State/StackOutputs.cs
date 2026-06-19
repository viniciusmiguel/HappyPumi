#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Reads the root stack outputs out of a stack's latest checkpoint. The checkpoint's <c>resources</c> array
/// contains the engine's resources; the root resource (<c>pulumi:pulumi:Stack</c>) carries the stack's
/// <c>outputs</c>. Used by the <c>fn::open::pulumi-stacks</c> ESC provider for cross-stack references.
/// </summary>
public static class StackOutputs
{
    private const string StackType = "pulumi:pulumi:Stack";
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Extracts the root stack's outputs, or an empty map when the stack has no checkpoint/outputs.</summary>
    public static Dictionary<string, object?> Extract(AppUntypedDeployment? deployment)
    {
        var result = new Dictionary<string, object?>();
        if (deployment?.Deployment is null)
            return result;

        var json = JsonSerializer.Serialize(deployment.Deployment, Options);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("resources", out var resources) || resources.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var resource in resources.EnumerateArray())
        {
            if (resource.TryGetProperty("type", out var type) && type.GetString() == StackType
                && resource.TryGetProperty("outputs", out var outputs) && outputs.ValueKind == JsonValueKind.Object)
                return (Dictionary<string, object?>)Normalize(outputs)!;
        }
        return result;
    }

    // Convert a JsonElement tree to plain dictionaries/lists/scalars so it serializes back cleanly.
    private static object? Normalize(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => Normalize(p.Value)),
        JsonValueKind.Array => el.EnumerateArray().Select(Normalize).ToList(),
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };
}
