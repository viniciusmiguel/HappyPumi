#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HappyPumi.Api.Contracts;
using YamlDotNet.Serialization;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>Sentinel for a value the static evaluator cannot resolve without opening (a live <c>fn::open</c>).</summary>
public sealed class EscUnknown
{
    public static readonly EscUnknown Instance = new();
    private EscUnknown() { }
}

/// <summary>
/// The ESC environment evaluator: resolves <c>${a.b.c}</c> interpolations against the <c>values</c> tree,
/// honours <c>fn::secret</c>, and applies the non-provider built-ins (<see cref="EscBuiltins"/>). Provider
/// invocations (<c>fn::open</c>) are pre-resolved by <c>EscOpener</c> before evaluation; any that remain at
/// evaluation time (e.g. on a plain <c>check</c>) become <see cref="EscValue.Unknown"/>. Behavior is ported
/// from the Apache-2.0 <c>pulumi/esc</c> evaluator (ADR-0008).
/// </summary>
public static class EnvironmentEvaluator
{
    private const int MaxDepth = 32; // guards against self-referential definitions
    private static readonly Regex Interp = new(@"\$\{([^}]+)\}", RegexOptions.Compiled);
    private static readonly Regex WholeInterp = new(@"^\$\{([^}]+)\}$", RegexOptions.Compiled);
    private static readonly IDeserializer Yaml = new DeserializerBuilder().Build();

    /// <summary>Evaluates a single definition's <c>values</c> tree (no imports, no provider execution).</summary>
    /// <example><c>EnvironmentEvaluator.Evaluate("values:\n  region: ${aws.region}\n")</c></example>
    public static Dictionary<string, EscValue> Evaluate(string definitionYaml) =>
        EvaluateValues(ValuesOf(ParseRoot(definitionYaml)));

    /// <summary>Evaluates an already-merged, already-provider-resolved <c>values</c> tree.</summary>
    public static Dictionary<string, EscValue> EvaluateValues(Dictionary<string, object?> values)
    {
        var result = new Dictionary<string, EscValue>();
        foreach (var (key, node) in values)
            result[key] = ToEscValue(node, values, 0);
        return result;
    }

    /// <summary>Parses a definition into its normalized root map (string keys), with <c>imports</c>/<c>values</c>.</summary>
    public static Dictionary<string, object?> ParseRoot(string definitionYaml) =>
        Normalize(Yaml.Deserialize<object?>(definitionYaml ?? "")) as Dictionary<string, object?>
        ?? new Dictionary<string, object?>();

    /// <summary>Extracts the <c>values</c> sub-tree from a parsed root.</summary>
    public static Dictionary<string, object?> ValuesOf(Dictionary<string, object?> root) =>
        root.TryGetValue("values", out var v) && v is Dictionary<string, object?> m
            ? m : new Dictionary<string, object?>();

    /// <summary>Resolves a node to a plain value against <paramref name="rootValues"/> (interpolations + built-ins).</summary>
    public static object? ResolveNode(object? node, Dictionary<string, object?> rootValues) =>
        ResolvePlain(node, rootValues, 0);

    private static EscValue ToEscValue(object? node, Dictionary<string, object?> rootValues, int depth)
    {
        if (depth > MaxDepth)
            return new EscValue { Value = null };

        if (node is Dictionary<string, object?> map && IsFunction(map, out var fn, out var arg))
            return FunctionToEscValue(fn, arg, rootValues, depth);

        if (node is Dictionary<string, object?> obj)
        {
            var nested = new Dictionary<string, EscValue>();
            foreach (var (k, child) in obj)
                nested[k] = ToEscValue(child, rootValues, depth + 1);
            return new EscValue { Value = nested };
        }

        if (node is List<object?> list)
            return new EscValue { Value = list.Select(e => ResolvePlain(e, rootValues, depth + 1)).ToList() };

        if (node is string s)
            return new EscValue { Value = Interpolate(s, rootValues, depth) };

        return new EscValue { Value = node };
    }

    private static EscValue FunctionToEscValue(string fn, object? arg, Dictionary<string, object?> root, int depth)
    {
        if (fn == "fn::secret")
            return new EscValue { Value = ResolvePlain(arg, root, depth + 1), Secret = true };
        if (fn.StartsWith("fn::open"))
            return new EscValue { Unknown = true }; // a live provider value, only resolvable by opening
        return new EscValue { Value = EscBuiltins.Apply(fn, ResolvePlain(arg, root, depth + 1)) };
    }

    // Resolve a node to plain values (used for interpolation lookups, list elements and built-in args).
    private static object? ResolvePlain(object? node, Dictionary<string, object?> rootValues, int depth)
    {
        if (depth > MaxDepth) return null;
        if (node is Dictionary<string, object?> map && IsFunction(map, out var fn, out var arg))
        {
            if (fn == "fn::secret") return ResolvePlain(arg, rootValues, depth + 1);
            if (fn.StartsWith("fn::open")) return EscUnknown.Instance;
            return EscBuiltins.Apply(fn, ResolvePlain(arg, rootValues, depth + 1));
        }
        return node switch
        {
            Dictionary<string, object?> obj => obj.ToDictionary(kv => kv.Key, kv => ResolvePlain(kv.Value, rootValues, depth + 1)),
            List<object?> list => list.Select(e => ResolvePlain(e, rootValues, depth + 1)).ToList(),
            string s => Interpolate(s, rootValues, depth),
            _ => node,
        };
    }

    // Substitute ${path} references. A whole-string reference yields the referenced value (any type);
    // embedded references are stringified into the surrounding text.
    private static object? Interpolate(string text, Dictionary<string, object?> rootValues, int depth)
    {
        var whole = WholeInterp.Match(text);
        if (whole.Success)
            return Lookup(whole.Groups[1].Value.Trim(), rootValues, depth);

        return Interp.Replace(text, m =>
        {
            var resolved = Lookup(m.Groups[1].Value.Trim(), rootValues, depth);
            return resolved?.ToString() ?? "";
        });
    }

    private static object? Lookup(string path, Dictionary<string, object?> rootValues, int depth)
    {
        if (depth > MaxDepth) return null;
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

    // A function node is a single-key map whose key is an "fn::" function (fn::secret, fn::open*, built-ins).
    private static bool IsFunction(Dictionary<string, object?> map, out string fn, out object? arg)
    {
        fn = "";
        arg = null;
        if (map.Count != 1) return false;
        var key = map.Keys.First();
        if (!key.StartsWith("fn::")) return false;
        fn = key;
        arg = map[key];
        return true;
    }

    // YamlDotNet yields Dictionary<object,object> / List<object> / scalars; normalize keys to strings.
    private static object? Normalize(object? node) => node switch
    {
        IDictionary<object, object> d => d.ToDictionary(kv => kv.Key?.ToString() ?? "", kv => Normalize(kv.Value)),
        IList<object> l => l.Select(Normalize).ToList(),
        _ => node,
    };
}
