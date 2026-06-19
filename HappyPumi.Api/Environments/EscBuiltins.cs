#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// The non-provider ESC built-in functions (<c>fn::join</c>, <c>fn::toJSON</c>, <c>fn::fromJSON</c>,
/// <c>fn::toBase64</c>, <c>fn::fromBase64</c>, <c>fn::toString</c>). Behavior is ported from the
/// Apache-2.0 <c>pulumi/esc</c> evaluator (ADR-0008; see reverse-engineering notes). Each function
/// receives its argument already resolved to a plain value (interpolations expanded) and returns a
/// plain value; <c>fn::secret</c> and <c>fn::open</c> are handled by the evaluator/opener, not here.
/// </summary>
public static class EscBuiltins
{
    private static readonly HashSet<string> Names = new()
    {
        "fn::join", "fn::toJSON", "fn::fromJSON", "fn::toBase64", "fn::fromBase64", "fn::toString",
    };

    public static bool IsBuiltin(string fnName) => Names.Contains(fnName);

    /// <summary>Applies a built-in to its already-resolved argument.</summary>
    public static object? Apply(string fnName, object? arg) => fnName switch
    {
        "fn::join" => Join(arg),
        "fn::toJSON" => ToJson(arg),
        "fn::fromJSON" => FromJson(arg),
        "fn::toBase64" => ToBase64(arg),
        "fn::fromBase64" => FromBase64(arg),
        "fn::toString" => Stringify(arg),
        _ => throw new ArgumentException($"Unknown ESC built-in '{fnName}'. Expected one of: {string.Join(", ", Names)}."),
    };

    // fn::join expects [separator, [parts...]]; parts are stringified and concatenated with the separator.
    private static string Join(object? arg)
    {
        if (arg is not List<object?> pair || pair.Count != 2 || pair[1] is not List<object?> parts)
            throw new ArgumentException(
                $"fn::join expects [separator, [parts...]]; got {Describe(arg)}.");
        var separator = pair[0]?.ToString() ?? "";
        return string.Join(separator, parts.Select(Stringify));
    }

    private static string ToJson(object? arg) => JsonSerializer.Serialize(arg);

    private static object? FromJson(object? arg)
    {
        if (arg is not string json)
            throw new ArgumentException($"fn::fromJSON expects a string; got {Describe(arg)}.");
        return Normalize(JsonSerializer.Deserialize<JsonElement>(json));
    }

    private static string ToBase64(object? arg)
    {
        if (arg is not string s)
            throw new ArgumentException($"fn::toBase64 expects a string; got {Describe(arg)}.");
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
    }

    private static string FromBase64(object? arg)
    {
        if (arg is not string s)
            throw new ArgumentException($"fn::fromBase64 expects a string; got {Describe(arg)}.");
        return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }

    private static string Stringify(object? arg) => arg switch
    {
        null => "",
        string s => s,
        bool b => b ? "true" : "false",
        Dictionary<string, object?> or List<object?> => JsonSerializer.Serialize(arg),
        _ => arg.ToString() ?? "",
    };

    // Map a parsed JSON element back onto the evaluator's plain shapes (Dictionary/List/scalar).
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

    private static string Describe(object? value) =>
        value is null ? "null" : $"{value.GetType().Name} ({value})";
}
