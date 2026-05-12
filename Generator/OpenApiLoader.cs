using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace PulumiApiGenerator;

public static class OpenApiLoader
{
    public static async Task<OpenApiDocument> LoadAsync(string source, CancellationToken ct = default)
    {
        byte[] bytes;

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("PulumiApiGenerator/1.0");
            bytes = await http.GetByteArrayAsync(uri, ct);
        }
        else
        {
            bytes = await File.ReadAllBytesAsync(source, ct);
        }

        using var stream = new MemoryStream(bytes);
        var reader = new OpenApiStreamReader(new OpenApiReaderSettings
        {
            // Don't try to follow remote $refs while reading — the Pulumi spec is fully inlined.
            ReferenceResolution = ReferenceResolutionSetting.ResolveLocalReferences,
        });

        var doc = reader.Read(stream, out var diagnostic);

        if (diagnostic.Errors.Count > 0)
        {
            Console.Error.WriteLine("OpenAPI parse errors:");
            foreach (var e in diagnostic.Errors)
                Console.Error.WriteLine($"  - {e.Message} ({e.Pointer})");
        }
        if (diagnostic.Warnings.Count > 0)
        {
            // Warnings are common and usually harmless; print to stderr but continue.
            Console.Error.WriteLine($"OpenAPI parse warnings: {diagnostic.Warnings.Count}");
        }

        return doc;
    }
}
