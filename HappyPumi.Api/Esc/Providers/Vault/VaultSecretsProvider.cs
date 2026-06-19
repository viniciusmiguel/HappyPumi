#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Esc.Providers.Vault;

/// <summary>
/// The <c>fn::open::vault-secrets</c> provider: reads KV-v2 secrets from HashiCorp Vault at open time.
/// Inputs (already interpolated by the opener):
/// <code>
/// address: https://vault.example.com
/// token: { fn::secret: ... }     # optional; else an ambient Vault token
/// mount: secret                  # KV v2 mount, default "secret"
/// get:
///   apiKey: { path: app/config, field: api_key }
/// </code>
/// Output is <c>{ apiKey: {fn::secret: ...}, ... }</c> — every value is secret.
/// </summary>
public sealed class VaultSecretsProvider(IVaultClient client) : IEscProvider
{
    public string Name => "vault-secrets";

    public string Description => "Reads KV-v2 secrets from HashiCorp Vault at open time via fn::open::vault-secrets.";

    public EscSchemaSchema Inputs => new()
    {
        Type = "object",
        Required = new List<string> { "address", "get" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["address"] = new() { Type = "string", Description = "Vault server address, e.g. https://vault.example.com." },
            ["token"] = new() { Type = "string", Description = "Vault token (optional if an ambient token is configured).", Secret = true },
            ["mount"] = new() { Type = "string", Description = "KV v2 mount path (default 'secret')." },
            ["get"] = new() { Type = "object", Description = "Map of output key to { path, field }.", AdditionalProperties = new() { Type = "object" } },
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
        var address = EscProviderInputs.Require<string>(inputs, Name, "address");
        var token = inputs.GetValueOrDefault("token") as string;
        var mount = inputs.GetValueOrDefault("mount") as string ?? "secret";
        var get = EscProviderInputs.Require<Dictionary<string, object?>>(inputs, Name, "get");

        var output = new Dictionary<string, object?>();
        foreach (var (outputKey, spec) in get)
        {
            var (path, field) = ParseSpec(spec, outputKey);
            var value = await client.ReadAsync(new VaultSecretRef(address, token, mount, path, field), ct);
            output[outputKey] = EscProviderInputs.Secret(value);
        }
        return output;
    }

    private static (string Path, string Field) ParseSpec(object? spec, string outputKey)
    {
        if (spec is not Dictionary<string, object?> map
            || map.GetValueOrDefault("path") is not string path
            || map.GetValueOrDefault("field") is not string field)
            throw new ArgumentException(
                $"vault-secrets 'get.{outputKey}' must be an object with 'path' and 'field'; got {EscProviderInputs.Describe(spec)}.");
        return (path, field);
    }
}
