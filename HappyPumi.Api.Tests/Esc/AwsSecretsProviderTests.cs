using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.AwsSecrets;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the fn::open::aws-secrets provider (against a fake Secrets Manager client).</summary>
public sealed class AwsSecretsProviderTests
{
    private const string Region = "us-east-1";

    private static Dictionary<string, object?> Get(string outputKey, Dictionary<string, object?> spec)
        => new() { ["region"] = Region, ["get"] = new Dictionary<string, object?> { [outputKey] = spec } };

    [Fact]
    public async Task ReadsSecretAndMarksItSecret()
    {
        var client = new FakeAwsSecretsClient().With(Region, "prod/api-key", "s3cr3t");
        var provider = new AwsSecretsProvider(client);

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(
            Get("apiKey", new Dictionary<string, object?> { ["secretId"] = "prod/api-key" }), CancellationToken.None))!;

        var apiKey = (Dictionary<string, object?>)output["apiKey"]!;
        Assert.Equal("s3cr3t", apiKey["fn::secret"]);
    }

    [Fact]
    public async Task ExtractsJsonKeyFromSecret()
    {
        var client = new FakeAwsSecretsClient().With(Region, "prod/db", "{\"password\":\"pw\",\"user\":\"admin\"}");
        var provider = new AwsSecretsProvider(client);

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(
            Get("db", new Dictionary<string, object?> { ["secretId"] = "prod/db", ["jsonKey"] = "password" }), CancellationToken.None))!;

        var db = (Dictionary<string, object?>)output["db"]!;
        Assert.Equal("pw", db["fn::secret"]);
    }

    [Fact]
    public async Task MissingRegionThrows()
    {
        var provider = new AwsSecretsProvider(new FakeAwsSecretsClient());
        var inputs = new Dictionary<string, object?> { ["get"] = new Dictionary<string, object?>() };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("region", ex.Message);
    }

    [Fact]
    public async Task SpecWithoutSecretIdThrows()
    {
        var provider = new AwsSecretsProvider(new FakeAwsSecretsClient());
        var inputs = Get("apiKey", new Dictionary<string, object?>());
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("apiKey", ex.Message);
    }
}
