#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Esc.Providers.AzureKeyVault;

/// <summary>
/// The <c>fn::open::azure-keyvault</c> provider: reads named secrets from an Azure Key Vault at open time.
/// Inputs (already interpolated by the opener):
/// <code>
/// vault: https://my-vault.vault.azure.net   # or a bare name -> https://&lt;name&gt;.vault.azure.net
/// login: { clientId, clientSecret, tenantId }   # optional; else ambient Azure credentials
/// get:
///   apiKey:    { name: api-key }
///   dbPassword:{ name: db-password, version: &lt;optional&gt; }
/// </code>
/// Output is <c>{ apiKey: {fn::secret: ...}, dbPassword: {fn::secret: ...} }</c> — every vault value is secret.
/// </summary>
public sealed class AzureKeyVaultProvider(IAzureKeyVaultClient client) : IEscProvider
{
    public string Name => "azure-keyvault";

    public string Description => "Reads secrets from an Azure Key Vault at open time via fn::open::azure-keyvault.";

    public EscSchemaSchema Inputs => new()
    {
        Type = "object",
        Required = new List<string> { "vault", "get" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["vault"] = new() { Type = "string", Description = "Key Vault URL, or a bare name resolved to https://<name>.vault.azure.net." },
            ["login"] = new() { Type = "object", Description = "Optional service-principal credentials (clientId, clientSecret, tenantId); ambient credentials otherwise." },
            ["get"] = new() { Type = "object", Description = "Map of output key to { name, version? } identifying each secret to read.", AdditionalProperties = new() { Type = "object" } },
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
        var vaultUrl = NormalizeVaultUrl(Require<string>(inputs, "vault"));
        var login = ParseLogin(inputs);
        var get = Require<Dictionary<string, object?>>(inputs, "get");

        var output = new Dictionary<string, object?>();
        foreach (var (outputKey, spec) in get)
        {
            var reference = ToReference(vaultUrl, login, spec, outputKey);
            var value = await client.GetSecretAsync(reference, ct);
            output[outputKey] = new Dictionary<string, object?> { ["fn::secret"] = value };
        }
        return output;
    }

    private static AzureKeyVaultRef ToReference(string vaultUrl, AzureKeyVaultLogin? login, object? spec, string outputKey)
    {
        if (spec is not Dictionary<string, object?> map || map.GetValueOrDefault("name") is not string name)
            throw new ArgumentException($"azure-keyvault 'get.{outputKey}' must be an object with a 'name'; got {Describe(spec)}.");
        var version = map.GetValueOrDefault("version") as string;
        return new AzureKeyVaultRef(vaultUrl, name, version, login);
    }

    private static AzureKeyVaultLogin? ParseLogin(IReadOnlyDictionary<string, object?> inputs)
    {
        if (inputs.GetValueOrDefault("login") is not Dictionary<string, object?> login)
            return null;
        return new AzureKeyVaultLogin(
            login.GetValueOrDefault("clientId") as string,
            login.GetValueOrDefault("clientSecret") as string,
            login.GetValueOrDefault("tenantId") as string);
    }

    private static string NormalizeVaultUrl(string vault) =>
        vault.Contains("://", StringComparison.Ordinal) ? vault : $"https://{vault}.vault.azure.net";

    private static T Require<T>(IReadOnlyDictionary<string, object?> inputs, string key) where T : class =>
        inputs.GetValueOrDefault(key) as T
        ?? throw new ArgumentException(
            $"azure-keyvault requires '{key}' of type {typeof(T).Name}; got {Describe(inputs.GetValueOrDefault(key))}.");

    private static string Describe(object? value) => value is null ? "null" : $"{value.GetType().Name} ({value})";
}
