#nullable enable

using System;
using System.IO;
using System.Reflection;

namespace HappyPumi.Api.Miscellaneous;

/// <summary>
/// Reads the Pulumi Cloud OpenAPI contract that ships embedded in this assembly (see the
/// <c>EmbeddedResource</c> in HappyPumi.Api.csproj). It is the verbatim source served by
/// <c>FetchRestSpecification</c> at <c>GET /api/openapi/pulumi-spec.json</c>.
/// </summary>
/// <example><code>using var spec = OpenApiSpecResource.OpenRead(); await spec.CopyToAsync(output);</code></example>
public static class OpenApiSpecResource
{
    /// <summary>Manifest key declared via &lt;LogicalName&gt; in the project file.</summary>
    public const string ResourceName = "HappyPumi.Api.pulumi-spec.json";

    /// <summary>
    /// Opens the embedded spec for reading. Throws <see cref="InvalidOperationException"/> naming the
    /// missing resource if the build did not embed it, so the failure is diagnosable rather than a
    /// silent null at the call site.
    /// </summary>
    public static Stream OpenRead()
    {
        var assembly = typeof(OpenApiSpecResource).Assembly;
        return assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded OpenAPI spec '{ResourceName}' was not found in assembly '{assembly.GetName().Name}'. " +
                "Expected it to be embedded via <EmbeddedResource Include=\"..\\pulumi-spec.json\"> in HappyPumi.Api.csproj.");
    }
}
