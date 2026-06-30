#nullable enable

using System.Globalization;

namespace HappyPumi.Api.Endpoints.Stacks;

/// <summary>
/// Builds the short human-readable summary string the web console shows for an update, e.g.
/// <c>"update #3 succeeded · 4 resources"</c>.
/// </summary>
internal static class ConsoleUpdateSummaryText
{
    public static string For(long version, string result, long resourceCount)
    {
        var noun = resourceCount == 1 ? "resource" : "resources";
        return string.Create(CultureInfo.InvariantCulture, $"update #{version} {result} · {resourceCount} {noun}");
    }
}
