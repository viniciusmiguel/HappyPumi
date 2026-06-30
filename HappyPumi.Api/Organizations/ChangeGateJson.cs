#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Serialization for the change-gate contracts. The generated polymorphic contracts (e.g.
/// <c>ChangeGateRuleInput</c>, <c>ApprovalRuleEligibilityInput</c>, <c>TargetEntity</c>) declare a CLR
/// property whose JSON name equals their <c>[JsonPolymorphic]</c> discriminator ("ruleType",
/// "eligibilityType", "entityType"). System.Text.Json rejects that collision
/// (<c>PropertyConflictsWithMetadataPropertyName</c>), so the default FastEndpoints binder 500s on these
/// bodies. We can't edit the generated contracts, so we use a resolver that drops the redundant property and
/// lets STJ own the discriminator. Reported as a PR1 generator quirk; see the PR description.
/// </summary>
public static class ChangeGateJson
{
    public static readonly JsonSerializerOptions Options = Build();

    /// <summary>Serializes a change-gate contract with the conflict-tolerant resolver.</summary>
    public static string Serialize(object value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Reads and deserializes the raw request body, bypassing the default polymorphic binder.</summary>
    public static async Task<T?> ReadAsync<T>(HttpContext ctx, CancellationToken ct)
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var raw = await reader.ReadToEndAsync(ct);
        return string.IsNullOrWhiteSpace(raw) ? default : JsonSerializer.Deserialize<T>(raw, Options);
    }

    private static JsonSerializerOptions Build()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(DropDiscriminatorProperty);
        return new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = resolver };
    }

    private static void DropDiscriminatorProperty(JsonTypeInfo info)
    {
        var name = DiscriminatorName(info.Type);
        if (name is null)
            return;
        var clash = info.Properties.FirstOrDefault(p => p.Name == name);
        if (clash is not null)
            info.Properties.Remove(clash);
    }

    // The [JsonPolymorphic] attribute lives on the base of each hierarchy and is not inherited, so we walk
    // the base chain explicitly to find the discriminator name for both base and derived contract types.
    private static string? DiscriminatorName(Type type)
    {
        for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
        {
            var poly = t.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false);
            if (poly is not null)
                return poly.TypeDiscriminatorPropertyName;
        }
        return null;
    }
}
