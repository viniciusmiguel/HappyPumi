#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Esc.Providers.GcpSecrets;

/// <summary>
/// The <c>fn::open::gcp-secrets</c> provider: reads secrets from Google Cloud Secret Manager at open time.
/// Inputs (already interpolated by the opener):
/// <code>
/// project: my-gcp-project
/// get:
///   apiKey: { secretId: api-key }
///   token:  { secretId: bot-token, version: "3" }   # version defaults to "latest"
/// </code>
/// Output is <c>{ apiKey: {fn::secret: ...}, ... }</c> — every value is secret.
/// </summary>
public sealed class GcpSecretsProvider(IGcpSecretsClient client) : IEscProvider
{
    public string Name => "gcp-secrets";

    public string Description => "Reads secrets from Google Cloud Secret Manager at open time via fn::open::gcp-secrets.";

    public EscSchemaSchema Inputs => new()
    {
        Type = "object",
        Required = new List<string> { "project", "get" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["project"] = new() { Type = "string", Description = "GCP project id." },
            ["get"] = new() { Type = "object", Description = "Map of output key to { secretId, version? }.", AdditionalProperties = new() { Type = "object" } },
        },
    };

    public EscSchemaSchema Outputs => new()
    {
        Type = "object",
        Description = "Map of the requested output keys to their secret values.",
        AdditionalProperties = new() { Type = "string", Secret = true },
    };

    public async Task<object?> OpenAsync(IReadOnlyDictionary<string, object?> inputs, CancellationToken ct)
    {
        var project = EscProviderInputs.Require<string>(inputs, Name, "project");
        var get = EscProviderInputs.Require<Dictionary<string, object?>>(inputs, Name, "get");

        var output = new Dictionary<string, object?>();
        foreach (var (outputKey, spec) in get)
        {
            var (secretId, version) = ParseSpec(spec, outputKey);
            var value = await client.AccessAsync(new GcpSecretRef(project, secretId, version), ct);
            output[outputKey] = EscProviderInputs.Secret(value);
        }
        return output;
    }

    private static (string SecretId, string? Version) ParseSpec(object? spec, string outputKey)
    {
        if (spec is not Dictionary<string, object?> map || map.GetValueOrDefault("secretId") is not string secretId)
            throw new ArgumentException(
                $"gcp-secrets 'get.{outputKey}' must be an object with a 'secretId'; got {EscProviderInputs.Describe(spec)}.");
        return (secretId, map.GetValueOrDefault("version") as string);
    }
}
