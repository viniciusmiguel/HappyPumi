#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Endpoints.Registry;

/// <summary>
/// Derives the API-docs navigation tree from a Pulumi package schema: groups the schema's resources and
/// functions (keyed by "pkg:module:Type" tokens) into modules with per-language name maps + type tokens —
/// the shape the console's nav tree consumes.
/// </summary>
public static class SchemaNav
{
    public static List<GetPackageNavModule> Derive(byte[] schemaJson)
    {
        using var doc = JsonDocument.Parse(schemaJson);
        var root = doc.RootElement;
        var modules = new Dictionary<string, GetPackageNavModule>();

        GetPackageNavModule Module(string name) =>
            modules.TryGetValue(name, out var m) ? m
            : modules[name] = new GetPackageNavModule { Name = Lang(name), Resources = new(), Functions = new() };

        void Add(string section, bool isResource)
        {
            if (!root.TryGetProperty(section, out var items) || items.ValueKind != JsonValueKind.Object)
                return;
            foreach (var entry in items.EnumerateObject())
            {
                var (module, type) = SplitToken(entry.Name);
                var item = new GetPackageNavItem { Name = Lang(type), TypeToken = entry.Name };
                if (isResource) Module(module).Resources!.Add(item);
                else Module(module).Functions!.Add(item);
            }
        }

        Add("resources", isResource: true);
        Add("functions", isResource: false);
        foreach (var m in modules.Values)
        {
            m.ResourcesTotal = m.Resources?.Count ?? 0;
            m.FunctionsTotal = m.Functions?.Count ?? 0;
        }
        return modules.Values.ToList();
    }

    // "pkg:module:Type" -> (module, Type). Falls back to "index" when no module segment is present.
    private static (string Module, string Type) SplitToken(string token)
    {
        var parts = token.Split(':');
        return parts.Length switch
        {
            >= 3 => (string.IsNullOrEmpty(parts[1]) ? "index" : parts[1], parts[^1]),
            2 => ("index", parts[^1]),
            _ => ("index", token),
        };
    }

    private static Dictionary<string, string> Lang(string name) => new()
    {
        ["go"] = name, ["nodejs"] = name, ["python"] = name, ["dotnet"] = name,
    };
}
