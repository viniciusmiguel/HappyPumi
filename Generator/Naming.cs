using System.Text;
using System.Text.RegularExpressions;

namespace PulumiApiGenerator;

/// <summary>
/// Helpers for turning OpenAPI names (camelCase, snake_case, kebab-case, dotted, ...)
/// into valid C# identifiers.
/// </summary>
public static class Naming
{
    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract","as","base","bool","break","byte","case","catch","char","checked",
        "class","const","continue","decimal","default","delegate","do","double","else",
        "enum","event","explicit","extern","false","finally","fixed","float","for",
        "foreach","goto","if","implicit","in","int","interface","internal","is","lock",
        "long","namespace","new","null","object","operator","out","override","params",
        "private","protected","public","readonly","ref","return","sbyte","sealed",
        "short","sizeof","stackalloc","static","string","struct","switch","this","throw",
        "true","try","typeof","uint","ulong","unchecked","unsafe","ushort","using",
        "virtual","void","volatile","while",
    };

    private static readonly Regex Splitter =
        new(@"[^A-Za-z0-9]+|(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

    public static string PascalCase(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Unnamed";

        var sb = new StringBuilder();
        foreach (var part in Splitter.Split(raw))
        {
            if (string.IsNullOrEmpty(part)) continue;
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1) sb.Append(part[1..].ToLowerInvariant());
        }

        var result = sb.Length == 0 ? "Unnamed" : sb.ToString();
        if (char.IsDigit(result[0])) result = "_" + result;
        return result;
    }

    public static string CamelCase(string raw)
    {
        var p = PascalCase(raw);
        return p.Length == 0 ? p : char.ToLowerInvariant(p[0]) + p[1..];
    }

    /// <summary>Type name (class/enum/struct).</summary>
    public static string TypeName(string raw) => Safe(PascalCase(raw));

    /// <summary>Property name. Property names sit in a different scope from keywords for most uses, but @-prefix anyway to be safe.</summary>
    public static string PropertyName(string raw, string? containingTypeName = null)
    {
        var n = PascalCase(raw);
        // C# disallows a member having the same name as its enclosing type.
        if (containingTypeName != null && n == containingTypeName) n += "Value";
        return Safe(n);
    }

    /// <summary>Parameter / variable name.</summary>
    public static string ParameterName(string raw) => Safe(CamelCase(raw));

    private static string Safe(string id) =>
        ReservedKeywords.Contains(id) ? "@" + id : id;

    /// <summary>String literal escape for use inside double-quoted C# string.</summary>
    public static string EscapeStringLiteral(string s)
    {
        var sb = new StringBuilder(s.Length + 8);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"':  sb.Append("\\\""); break;
                case '\r': sb.Append("\\r");  break;
                case '\n': sb.Append("\\n");  break;
                case '\t': sb.Append("\\t");  break;
                default:
                    if (c < 0x20) sb.Append($"\\u{(int)c:X4}");
                    else sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
