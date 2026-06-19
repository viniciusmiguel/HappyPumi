using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.GcpSecrets;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the fn::open::gcp-secrets provider (against a fake Secret Manager client).</summary>
public sealed class GcpSecretsProviderTests
{
    private const string Project = "my-project";

    private static Dictionary<string, object?> Get(string outputKey, Dictionary<string, object?> spec)
        => new() { ["project"] = Project, ["get"] = new Dictionary<string, object?> { [outputKey] = spec } };

    [Fact]
    public async Task ReadsSecretAndMarksItSecret()
    {
        var client = new FakeGcpSecretsClient().With(Project, "api-key", "g-s3cr3t");
        var provider = new GcpSecretsProvider(client);

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(
            Get("apiKey", new Dictionary<string, object?> { ["secretId"] = "api-key" }), CancellationToken.None))!;

        var apiKey = (Dictionary<string, object?>)output["apiKey"]!;
        Assert.Equal("g-s3cr3t", apiKey["fn::secret"]);
    }

    [Fact]
    public async Task VersionIsPassedThrough()
    {
        var client = new FakeGcpSecretsClient().With(Project, "bot", "t");
        var provider = new GcpSecretsProvider(client);

        await provider.OpenAsync(Get("token", new Dictionary<string, object?> { ["secretId"] = "bot", ["version"] = "3" }), CancellationToken.None);

        Assert.Equal("3", client.Requests.Single().Version);
    }

    [Fact]
    public async Task MissingProjectThrows()
    {
        var provider = new GcpSecretsProvider(new FakeGcpSecretsClient());
        var inputs = new Dictionary<string, object?> { ["get"] = new Dictionary<string, object?>() };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("project", ex.Message);
    }

    [Fact]
    public async Task SpecWithoutSecretIdThrows()
    {
        var provider = new GcpSecretsProvider(new FakeGcpSecretsClient());
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.OpenAsync(Get("apiKey", new Dictionary<string, object?>()), CancellationToken.None));
        Assert.Contains("apiKey", ex.Message);
    }
}
