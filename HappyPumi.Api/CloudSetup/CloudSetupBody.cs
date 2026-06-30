#nullable enable

using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using Microsoft.AspNetCore.Http;

namespace HappyPumi.Api.CloudSetup;

/// <summary>
/// Helpers shared by the cloud-setup endpoints (PR6). The generated POST request types have a
/// self-referential <c>Body</c> property (a generator quirk), so those endpoints are bodyless and parse the
/// JSON request body manually here. Also centralizes the connected-account "record setup" upsert so the
/// four setup endpoints don't duplicate it (CLAUDE.md: no code duplication).
/// </summary>
internal static class CloudSetupBody
{
    /// <summary>Reads the request body as a JSON object; returns an Undefined element for empty/invalid bodies.</summary>
    public static async Task<JsonElement> ReadAsync(HttpContext ctx, CancellationToken ct)
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var raw = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(raw))
            return default;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>Reads a string field from a parsed body, or null when absent / not a string.</summary>
    public static string? Str(JsonElement root, string field)
        => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(field, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    /// <summary>
    /// Records that a provider was set up for the org (upserting its connected-account row while preserving
    /// any already-discovered accounts), and returns a success result.
    /// </summary>
    public static CloudSetupResult RecordSetup(IConnectedCloudAccountStore store, string org, string provider, string message)
    {
        store.Upsert(org, provider, store.List(org, provider), credential: null);
        return new CloudSetupResult { Success = true, Message = message, Resources = new() };
    }
}
