#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Builds the <c>exprs</c> AST (a map of <see cref="EscExpr"/> keyed by top-level value) that the esc/pulumi
/// CLI walks to resolve a path on <c>env get &lt;path&gt;</c> (see esc env_get getEnvExpr). It mirrors the parsed
/// definition's <c>values</c> tree: maps become object exprs, lists become list exprs, scalars become literals.
/// (fn:: nodes are represented as plain objects, which is enough for path-to-node resolution.)
/// </summary>
public static class EscExprBuilder
{
    public static Dictionary<string, EscExpr> Build(Dictionary<string, object?> values)
        => values.ToDictionary(kv => kv.Key, kv => Node(kv.Value));

    private static EscExpr Node(object? value) => value switch
    {
        Dictionary<string, object?> map => new EscExpr { Object = map.ToDictionary(kv => kv.Key, kv => Node(kv.Value)), Range = Zero() },
        List<object?> list => new EscExpr { List = list.Select(Node).ToList(), Range = Zero() },
        _ => new EscExpr { Literal = value, Range = Zero() },
    };

    private static EscRange Zero() => new() { Begin = new EscPos(), End = new EscPos() };
}
