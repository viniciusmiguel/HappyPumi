using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace PulumiApiGenerator.Generators;

/// <summary>
/// Generates DTO classes and enums from <c>#/components/schemas</c>.
///
///  - <c>type: string</c> + <c>enum</c>  =&gt;  a C# <c>enum</c>.
///  - <c>type: object</c>                =&gt;  a C# class with auto-properties.
///  - <c>allOf: [{$ref}, {inline}]</c>   =&gt;  the inline properties are merged
///                                              into a class that inherits from
///                                              the referenced type.
///  - <c>discriminator</c>               =&gt;  emits <c>[JsonPolymorphic]</c> +
///                                              <c>[JsonDerivedType]</c> attrs so
///                                              <c>System.Text.Json</c> handles
///                                              polymorphic deserialization.
/// </summary>
public sealed class ContractGenerator
{
    private readonly OpenApiDocument _doc;
    private readonly string _rootNs;
    private readonly TypeMapper _mapper;

    public ContractGenerator(OpenApiDocument doc, string rootNs)
    {
        _doc = doc;
        _rootNs = rootNs;
        _mapper = new TypeMapper(doc);
    }

    public void Generate(string outDir)
    {
        Directory.CreateDirectory(outDir);
        if (_doc.Components?.Schemas is not { } schemas) return;

        var ns = $"{_rootNs}.Contracts";

        foreach (var (rawName, schema) in schemas)
        {
            var typeName = Naming.TypeName(rawName);
            string code;

            if (schema.Type == "string" && schema.Enum is { Count: > 0 })
            {
                code = WriteEnum(ns, typeName, schema);
            }
            else
            {
                code = WriteClass(ns, typeName, schema);
            }

            File.WriteAllText(Path.Combine(outDir, $"{typeName}.cs"), code);
        }
    }

    // ---------- ENUM ----------

    private static string WriteEnum(string ns, string typeName, OpenApiSchema schema)
    {
        var w = new CodeWriter();
        Header(w, ns, includeJson: true);
        w.XmlDoc(schema.Description);
        w.Line("[JsonConverter(typeof(JsonStringEnumConverter))]");
        using (w.Block($"public enum {typeName}"))
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var any in schema.Enum)
            {
                if (any is not OpenApiString s) continue;
                var raw = s.Value;
                var member = Naming.PascalCase(raw);
                if (!seen.Add(member)) continue; // duplicate-collision guard

                if (member != raw)
                    w.Line($"[System.Runtime.Serialization.EnumMember(Value = \"{Naming.EscapeStringLiteral(raw)}\")]");
                w.Line($"{member},");
            }
        }
        return w.ToString();
    }

    // ---------- CLASS ----------

    private string WriteClass(string ns, string typeName, OpenApiSchema schema)
    {
        // Resolve inheritance + merged properties from allOf
        var (baseTypeName, effective) = FlattenAllOf(schema);

        var w = new CodeWriter();
        Header(w, ns, includeJson: true);
        w.XmlDoc(schema.Description);

        // Polymorphic discriminator (on the base class only)
        if (schema.Discriminator is { } disc && !string.IsNullOrEmpty(disc.PropertyName))
        {
            w.Line($"[JsonPolymorphic(TypeDiscriminatorPropertyName = \"{Naming.EscapeStringLiteral(disc.PropertyName)}\")]");
            if (disc.Mapping is { Count: > 0 })
            {
                foreach (var (discValue, refString) in disc.Mapping)
                {
                    // refString is e.g. "#/components/schemas/AgentBackendEventAssistantMessage"
                    var derivedName = ExtractSchemaName(refString);
                    if (derivedName is null) continue;
                    w.Line($"[JsonDerivedType(typeof({Naming.TypeName(derivedName)}), typeDiscriminator: \"{Naming.EscapeStringLiteral(discValue)}\")]");
                }
            }
        }

        var declaration = baseTypeName is null
            ? $"public class {typeName}"
            : $"public class {typeName} : {baseTypeName}";

        using (w.Block(declaration))
        {
            EmitProperties(w, typeName, effective);
        }

        return w.ToString();
    }

    /// <summary>
    /// For schemas built with <c>allOf: [{$ref}, {inline...}]</c>, returns the
    /// base C# type name and a synthetic <see cref="OpenApiSchema"/> containing
    /// only the inline (derived) properties. For ordinary schemas, returns
    /// <c>(null, schema)</c>.
    /// </summary>
    private static (string? baseTypeName, OpenApiSchema effective) FlattenAllOf(OpenApiSchema schema)
    {
        if (schema.AllOf is not { Count: > 0 }) return (null, schema);

        var baseRef = schema.AllOf.FirstOrDefault(s => s.Reference != null);
        var inlines = schema.AllOf.Where(s => s.Reference == null).ToList();

        if (baseRef is null) return (null, schema);

        var merged = new OpenApiSchema
        {
            Type = "object",
            Description = schema.Description,
        };
        // Carry over the discriminator from either source (rare on derived types).
        merged.Discriminator = schema.Discriminator;

        foreach (var inl in inlines)
        {
            foreach (var (k, v) in inl.Properties) merged.Properties[k] = v;
            foreach (var r in inl.Required)       merged.Required.Add(r);
        }
        // Some specs define properties directly on the outer schema in addition to allOf
        foreach (var (k, v) in schema.Properties) merged.Properties[k] = v;
        foreach (var r in schema.Required) merged.Required.Add(r);

        return (Naming.TypeName(baseRef.Reference!.Id), merged);
    }

    private void EmitProperties(CodeWriter w, string typeName, OpenApiSchema schema)
    {
        if (schema.Properties is null || schema.Properties.Count == 0) return;

        bool first = true;
        foreach (var (jsonName, propSchema) in schema.Properties)
        {
            if (!first) w.Line();
            first = false;

            var propName = Naming.PropertyName(jsonName, containingTypeName: typeName);
            var isRequired = schema.Required?.Contains(jsonName) == true;
            var typeExpr = _mapper.Map(propSchema, isNullable: !isRequired);

            w.XmlDoc(propSchema.Description);

            // Always emit JsonPropertyName so JSON casing differences (and any
            // sanitization we did) are explicit.
            w.Line($"[JsonPropertyName(\"{Naming.EscapeStringLiteral(jsonName)}\")]");

            var init = isRequired && TypeMapper.IsReferenceTypeExpression(typeExpr)
                ? " = default!;"
                : "";

            w.Line($"public {typeExpr} {propName} {{ get; set; }}{init}");
        }
    }

    // ---------- helpers ----------

    private static void Header(CodeWriter w, string ns, bool includeJson)
    {
        w.Line("// <auto-generated />");
        w.Line("// This file was generated by PulumiApiGenerator. Do not edit by hand.");
        w.Line("#nullable enable");
        w.Line();
        w.Line("using System;");
        w.Line("using System.Collections.Generic;");
        if (includeJson) w.Line("using System.Text.Json.Serialization;");
        w.Line();
        w.Line($"namespace {ns};");
        w.Line();
    }

    private static string? ExtractSchemaName(string refString)
    {
        // "#/components/schemas/Foo" -> "Foo"
        const string prefix = "#/components/schemas/";
        if (refString.StartsWith(prefix, StringComparison.Ordinal))
            return refString[prefix.Length..];
        return null;
    }
}
