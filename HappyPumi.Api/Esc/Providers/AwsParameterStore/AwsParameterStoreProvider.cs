#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc.Providers.AwsSecrets;

namespace HappyPumi.Api.Esc.Providers.AwsParameterStore;

/// <summary>
/// The <c>fn::open::aws-parameter-store</c> provider: reads AWS SSM Parameter Store parameters at open time.
/// Inputs (already interpolated by the opener):
/// <code>
/// region: us-east-1
/// login: { accessKeyId, secretAccessKey, sessionToken }   # optional; else the AWS default credential chain
/// get:
///   dbUrl:  { name: /prod/db/url }
///   apiKey: { name: /prod/api-key, withDecryption: true }  # withDecryption defaults to true
/// </code>
/// Output is <c>{ dbUrl: {fn::secret: ...}, ... }</c> — every value is secret.
/// </summary>
public sealed class AwsParameterStoreProvider(IAwsParameterStoreClient client) : IEscProvider
{
    public string Name => "aws-parameter-store";

    public string Description => "Reads AWS SSM Parameter Store parameters at open time via fn::open::aws-parameter-store.";

    public EscSchemaSchema Inputs => new()
    {
        Type = "object",
        Required = new List<string> { "region", "get" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["region"] = new() { Type = "string", Description = "AWS region, e.g. us-east-1." },
            ["login"] = new() { Type = "object", Description = "Optional explicit credentials; the default chain otherwise." },
            ["get"] = new() { Type = "object", Description = "Map of output key to { name, withDecryption? }.", AdditionalProperties = new() { Type = "object" } },
        },
    };

    public EscSchemaSchema Outputs => new()
    {
        Type = "object",
        Description = "Map of the requested output keys to their parameter values.",
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
            var (name, withDecryption) = ParseSpec(spec, outputKey);
            var value = await client.GetParameterAsync(new AwsParameterRef(region, name, withDecryption, login), ct);
            output[outputKey] = EscProviderInputs.Secret(value);
        }
        return output;
    }

    private static (string Name, bool WithDecryption) ParseSpec(object? spec, string outputKey)
    {
        if (spec is not Dictionary<string, object?> map || map.GetValueOrDefault("name") is not string name)
            throw new ArgumentException(
                $"aws-parameter-store 'get.{outputKey}' must be an object with a 'name'; got {EscProviderInputs.Describe(spec)}.");
        // SecureString parameters need decryption; default to true so they resolve to plaintext.
        var withDecryption = map.GetValueOrDefault("withDecryption") is not bool b || b;
        return (name, withDecryption);
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
