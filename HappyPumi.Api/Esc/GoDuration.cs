#nullable enable

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HappyPumi.Api.Esc;

/// <summary>
/// Parses Go's <c>time.Duration</c> strings (e.g. <c>2h45m</c>, <c>300ms</c>, <c>1h</c>) — the format the ESC
/// <c>open?duration=</c> query parameter uses. Supports the units <c>ns, us/µs, ms, s, m, h</c>.
/// </summary>
public static class GoDuration
{
    private static readonly Regex Component = new(@"(\d+(?:\.\d+)?)(ns|us|µs|ms|s|m|h)", RegexOptions.Compiled);

    /// <summary>Parses <paramref name="text"/>, falling back to <paramref name="fallback"/> when null/empty/invalid.</summary>
    public static TimeSpan Parse(string? text, TimeSpan fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
            return fallback;

        var matches = Component.Matches(text);
        if (matches.Count == 0)
            return fallback;

        var total = TimeSpan.Zero;
        foreach (Match m in matches)
            total += UnitSpan(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), m.Groups[2].Value);
        return total;
    }

    private static TimeSpan UnitSpan(double value, string unit) => unit switch
    {
        "ns" => TimeSpan.FromTicks((long)(value / 100)), // 1 tick = 100ns
        "us" or "µs" => TimeSpan.FromTicks((long)(value * 10)),
        "ms" => TimeSpan.FromMilliseconds(value),
        "s" => TimeSpan.FromSeconds(value),
        "m" => TimeSpan.FromMinutes(value),
        "h" => TimeSpan.FromHours(value),
        _ => throw new ArgumentException($"Unsupported duration unit '{unit}' in Go duration.", nameof(unit)),
    };
}
