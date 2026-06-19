#nullable enable

using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Reads a JSON request body for the hand-written platform create endpoints. These are bodyless
/// (EndpointWithoutRequest) because the generated request types have self-referential <c>Body</c>
/// properties (a generator quirk), so they parse the body directly.
/// </summary>
public readonly struct PlatformBody
{
    private readonly JsonElement _root;
    private readonly bool _has;

    private PlatformBody(JsonElement root, bool has)
    {
        _root = root;
        _has = has;
    }

    public static async Task<PlatformBody> ReadAsync(HttpContext ctx, CancellationToken ct)
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var raw = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(raw))
            return new PlatformBody(default, false);
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return new PlatformBody(doc.RootElement.Clone(), true);
        }
        catch (JsonException)
        {
            return new PlatformBody(default, false);
        }
    }

    /// <summary>A string field, or null when absent/not a string.</summary>
    public string? Str(string field)
        => _has && _root.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    /// <summary>An int field, or <paramref name="fallback"/> when absent/not a number.</summary>
    public int Int(string field, int fallback)
        => _has && _root.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.Number
            ? el.GetInt32()
            : fallback;
}
