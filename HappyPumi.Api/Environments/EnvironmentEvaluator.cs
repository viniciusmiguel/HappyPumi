#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HappyPumi.Api.Contracts;
using YamlDotNet.Serialization;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// A focused ESC environment evaluator: parses the definition YAML, resolves <c>${a.b.c}</c> interpolations
/// against the <c>values</c> tree, and honours <c>fn::secret</c>. Produces the evaluated property tree the
/// console renders (a map of <see cref="EscValue"/>, where secret values are flagged).
/// </summary>
public static class EnvironmentEvaluator
{
    private static readonly Regex Interp = new(@"\$\{([^}]+)\}", RegexOptions.Compiled);
    private static readonly IDeserializer Yaml = new DeserializerBuilder().Build();

    /// <summary>Evaluates an environment definition into its resolved <c>properties</c> tree.</summary>
    public static Dictionary<string, EscValue> Evaluate(string definitionYaml)
    {
        var root = Normalize(Yaml.Deserialize<object?>(definitionYaml ?? "")) as Dictionary<string, object?>
                   ?? new Dictionary<string, object?>();
        var values = root.TryGetValue("values", out var v) && v is Dictionary<string, object?> m
            ? m : new Dictionary<string, object?>();

        var result = new Dictionary<string, EscValue>();
        foreach (var (key, node) in values)
            result[key] = ToEscValue(node, values, 0);
        return result;
    }

    private static EscValue ToEscValue(object? node, Dictionary<string, object?> rootValues, int depth)
    {
        if (depth > 32)
            return new EscValue { Value = null };

        if (node is Dictionary<string, object?> map)
        {
            if (map.Count == 1 && map.TryGetValue("fn::secret", out var secret))
                return new EscValue { Value = ResolvePlain(secret, rootValues, depth + 1), Secret = true };

            var nested = new Dictionary<string, EscValue>();
            foreach (var (k, child) in map)
                nested[k] = ToEscValue(child, rootValues, depth + 1);
            return new EscValue { Value = nested };
        }

        if (node is List<object?> list)
            return new EscValue { Value = list.Select(e => ResolvePlain(e, rootValues, depth + 1)).ToList() };

        if (node is string s)
            return new EscValue { Value = Interpolate(s, rootValues, depth) };

        return new EscValue { Value = node };
    }

    // Resolve a node to plain values (used for interpolation lookups and list elements).
    private static object? ResolvePlain(object? node, Dictionary<string, object?> rootValues, int depth)
    {
        if (depth > 32) return null;
        switch (node)
        {
            case Dictionary<string, object?> map when map.Count == 1 && map.ContainsKey("fn::secret"):
                return ResolvePlain(map["fn::secret"], rootValues, depth + 1);
            case Dictionary<string, object?> map:
                return map.ToDictionary(kv => kv.Key, kv => ResolvePlain(kv.Value, rootValues, depth + 1));
            case List<object?> list:
                return list.Select(e => ResolvePlain(e, rootValues, depth + 1)).ToList();
            case string s:
                return Interpolate(s, rootValues, depth);
            default:
                return node;
        }
    }

    // Substitute ${path} references. A whole-string reference yields the referenced value (any type);
    // embedded references are stringified into the surrounding text.
    private static object? Interpolate(string text, Dictionary<string, object?> rootValues, int depth)
    {
        var whole = Regex.Match(text, @"^\$\{([^}]+)\}$");
        if (whole.Success)
            return Lookup(whole.Groups[1].Value.Trim(), rootValues, depth);

        return Interp.Replace(text, mtch =>
        {
            var resolved = Lookup(mtch.Groups[1].Value.Trim(), rootValues, depth);
            return resolved?.ToString() ?? "";
        });
    }

    private static object? Lookup(string path, Dictionary<string, object?> rootValues, int depth)
    {
        if (depth > 32) return null;
        object? current = rootValues;
        foreach (var segment in path.Split('.'))
        {
            if (current is Dictionary<string, object?> map && map.TryGetValue(segment, out var next))
                current = next;
            else
                return null; // unresolved reference
        }
        return ResolvePlain(current, rootValues, depth + 1);
    }

    // YamlDotNet yields Dictionary<object,object> / List<object> / scalars; normalize keys to strings.
    private static object? Normalize(object? node) => node switch
    {
        IDictionary<object, object> d => d.ToDictionary(kv => kv.Key?.ToString() ?? "", kv => Normalize(kv.Value)),
        IList<object> l => l.Select(Normalize).ToList(),
        _ => node,
    };
}
