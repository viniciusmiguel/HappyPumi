using Microsoft.OpenApi.Models;

namespace PulumiApiGenerator;

/// <summary>
/// Maps OpenAPI <see cref="OpenApiSchema"/> values to C# type expressions
/// (e.g. <c>string</c>, <c>List&lt;Foo&gt;</c>, <c>Dictionary&lt;string, int&gt;</c>).
///
/// Nullability is decided by the caller (typically based on whether the property
/// is in the parent's <c>required</c> list), so the same schema can yield
/// <c>string</c> or <c>string?</c> depending on context.
/// </summary>
public sealed class TypeMapper
{
    private readonly OpenApiDocument _doc;

    /// <summary>Names that are C# value types (so callers can decide on init expressions).</summary>
    public static readonly HashSet<string> ValueTypes = new(StringComparer.Ordinal)
    {
        "bool","byte","sbyte","short","ushort","int","uint","long","ulong",
        "float","double","decimal","DateTime","DateTimeOffset","DateOnly","TimeOnly","Guid",
    };

    public TypeMapper(OpenApiDocument doc) { _doc = doc; }

    public string Map(OpenApiSchema? schema, bool isNullable)
    {
        var bare = MapCore(schema);
        return isNullable ? Nullify(bare) : bare;
    }

    private string MapCore(OpenApiSchema? schema)
    {
        if (schema is null) return "object";

        // Direct $ref
        if (schema.Reference is { } r && !string.IsNullOrEmpty(r.Id))
            return Naming.TypeName(r.Id);

        // allOf: prefer the first ref (used as a base class). Inline members are
        // merged into the derived class at generation time.
        if (schema.AllOf is { Count: > 0 })
        {
            var refSchema = schema.AllOf.FirstOrDefault(s => s.Reference != null);
            if (refSchema != null) return MapCore(refSchema);
            // Pure inline allOf — treat as object
            return "object";
        }

        // Arrays
        if (schema.Type == "array")
        {
            var inner = MapCore(schema.Items);
            return $"List<{inner}>";
        }

        // Free-form maps / dictionaries
        if (schema.Type == "object" && schema.AdditionalProperties != null)
        {
            var v = MapCore(schema.AdditionalProperties);
            return $"Dictionary<string, {v}>";
        }

        // Primitives
        return schema.Type switch
        {
            "string"  => MapString(schema),
            "integer" => schema.Format == "int64" ? "long" : "int",
            "number"  => schema.Format switch { "float" => "float", "decimal" => "decimal", _ => "double" },
            "boolean" => "bool",
            "object"  => "object",
            _ when schema.OneOf is { Count: > 0 } || schema.AnyOf is { Count: > 0 } => "object",
            _ => "object",
        };
    }

    private static string MapString(OpenApiSchema s) => s.Format switch
    {
        "date-time" => "DateTime",
        "date"      => "DateOnly",
        "time"      => "TimeOnly",
        "uuid"      => "Guid",
        "byte"      => "byte[]",
        "binary"    => "byte[]",
        _           => "string",
    };

    private static string Nullify(string t)
    {
        // byte[] -> byte[]?; List<T> -> List<T>?; int -> int?; etc.
        if (t.EndsWith("?", StringComparison.Ordinal)) return t;
        return t + "?";
    }

    public static bool IsReferenceTypeExpression(string t)
    {
        if (t.EndsWith("?", StringComparison.Ordinal)) t = t[..^1];
        if (t == "string" || t == "object" || t == "byte[]") return true;
        if (t.StartsWith("List<", StringComparison.Ordinal) ||
            t.StartsWith("Dictionary<", StringComparison.Ordinal)) return true;
        // Otherwise, a value-type name → not reference type; a generated PascalCase
        // class → reference type. We treat anything not in the known value-type
        // set as a reference type.
        return !ValueTypes.Contains(t);
    }
}
