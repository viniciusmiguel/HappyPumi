#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Esc.Providers.AwsSecrets;

/// <summary>
/// The <c>fn::open::aws-secrets</c> provider: reads secrets from AWS Secrets Manager at open time.
/// Inputs (already interpolated by the opener):
/// <code>
/// region: us-east-1
/// login: { accessKeyId, secretAccessKey, sessionToken }   # optional; else the AWS default credential chain
/// get:
///   apiKey:     { secretId: prod/api-key }
///   dbPassword: { secretId: prod/db, jsonKey: password }  # jsonKey extracts a field from a JSON secret
/// </code>
/// Output is <c>{ apiKey: {fn::secret: ...}, ... }</c> — every value is secret.
/// </summary>
public sealed class AwsSecretsProvider(IAwsSecretsClient client) : IEscProvider
{
    public string Name => "aws-secrets";

    public string Description => "Reads secrets from AWS Secrets Manager at open time via fn::open::aws-secrets.";

    public EscSchemaSchema Inputs => new()
    {
        Type = "object",
        Required = new List<string> { "region", "get" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["region"] = new() { Type = "string", Description = "AWS region, e.g. us-east-1." },
            ["login"] = new() { Type = "object", Description = "Optional explicit credentials (accessKeyId, secretAccessKey, sessionToken); the default chain otherwise." },
            ["get"] = new() { Type = "object", Description = "Map of output key to { secretId, jsonKey? }.", AdditionalProperties = new() { Type = "object" } },
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
        var region = EscProviderInputs.Require<string>(inputs, Name, "region");
        var login = ParseLogin(inputs);
        var get = EscProviderInputs.Require<Dictionary<string, object?>>(inputs, Name, "get");

        var output = new Dictionary<string, object?>();
        foreach (var (outputKey, spec) in get)
        {
            var (secretId, jsonKey) = ParseSpec(spec, outputKey);
            var raw = await client.GetSecretAsync(new AwsSecretRef(region, secretId, login), ct);
            output[outputKey] = EscProviderInputs.Secret(jsonKey is null ? raw : ExtractField(raw, jsonKey));
        }
        return output;
    }

    private static (string SecretId, string? JsonKey) ParseSpec(object? spec, string outputKey)
    {
        if (spec is not Dictionary<string, object?> map || map.GetValueOrDefault("secretId") is not string secretId)
            throw new ArgumentException(
                $"aws-secrets 'get.{outputKey}' must be an object with a 'secretId'; got {EscProviderInputs.Describe(spec)}.");
        return (secretId, map.GetValueOrDefault("jsonKey") as string);
    }

    // A SecretString may hold JSON; jsonKey selects one field from it.
    private static string? ExtractField(string? raw, string jsonKey)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;
        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement.TryGetProperty(jsonKey, out var value) ? value.ToString() : null;
    }

    private static AwsLogin? ParseLogin(IReadOnlyDictionary<string, object?> inputs)
    {
        if (inputs.GetValueOrDefault("login") is not Dictionary<string, object?> login)
            return null;
        return new AwsLogin(
            login.GetValueOrDefault("accessKeyId") as string,
            login.GetValueOrDefault("secretAccessKey") as string,
            login.GetValueOrDefault("sessionToken") as string);
    }
}
