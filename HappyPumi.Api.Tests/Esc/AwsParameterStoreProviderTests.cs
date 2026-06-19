using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.AwsParameterStore;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the fn::open::aws-parameter-store provider (against a fake SSM client).</summary>
public sealed class AwsParameterStoreProviderTests
{
    private const string Region = "us-east-1";

    private static Dictionary<string, object?> Get(string outputKey, Dictionary<string, object?> spec)
        => new() { ["region"] = Region, ["get"] = new Dictionary<string, object?> { [outputKey] = spec } };

    [Fact]
    public async Task ReadsParameterAndMarksItSecret()
    {
        var client = new FakeAwsParameterStoreClient().With(Region, "/prod/db/url", "postgres://x");
        var provider = new AwsParameterStoreProvider(client);

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(
            Get("dbUrl", new Dictionary<string, object?> { ["name"] = "/prod/db/url" }), CancellationToken.None))!;

        var dbUrl = (Dictionary<string, object?>)output["dbUrl"]!;
        Assert.Equal("postgres://x", dbUrl["fn::secret"]);
        Assert.True(client.Requests.Single().WithDecryption); // defaults to true
    }

    [Fact]
    public async Task MissingRegionThrows()
    {
        var provider = new AwsParameterStoreProvider(new FakeAwsParameterStoreClient());
        var inputs = new Dictionary<string, object?> { ["get"] = new Dictionary<string, object?>() };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("region", ex.Message);
    }

    [Fact]
    public async Task SpecWithoutNameThrows()
    {
        var provider = new AwsParameterStoreProvider(new FakeAwsParameterStoreClient());
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.OpenAsync(Get("dbUrl", new Dictionary<string, object?>()), CancellationToken.None));
        Assert.Contains("dbUrl", ex.Message);
    }
}
