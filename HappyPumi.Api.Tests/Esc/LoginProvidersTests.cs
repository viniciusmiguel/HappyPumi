using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.Logins;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the static-credential *-login providers.</summary>
public sealed class LoginProvidersTests
{
    [Fact]
    public async Task AwsLoginExposesCredentialsWithSecretsMarked()
    {
        var provider = new AwsLoginProvider();
        var inputs = new Dictionary<string, object?>
        {
            ["accessKeyId"] = "AKIA123",
            ["secretAccessKey"] = "shh",
            ["region"] = "us-east-1",
        };

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(inputs, CancellationToken.None))!;

        Assert.Equal("AKIA123", ((Dictionary<string, object?>)output["accessKeyId"]!)["fn::secret"]);
        Assert.Equal("shh", ((Dictionary<string, object?>)output["secretAccessKey"]!)["fn::secret"]);
        Assert.Equal("us-east-1", output["region"]); // non-secret, passed through plain
        Assert.False(output.ContainsKey("sessionToken")); // optional + absent -> omitted
    }

    [Fact]
    public async Task MissingRequiredCredentialThrows()
    {
        var provider = new AwsLoginProvider();
        var inputs = new Dictionary<string, object?> { ["accessKeyId"] = "AKIA123" }; // no secretAccessKey
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("secretAccessKey", ex.Message);
    }

    [Fact]
    public async Task VaultLoginMarksTokenSecretAndAddressPlain()
    {
        var provider = new VaultLoginProvider();
        var inputs = new Dictionary<string, object?> { ["address"] = "https://vault", ["token"] = "t" };

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(inputs, CancellationToken.None))!;

        Assert.Equal("https://vault", output["address"]);
        Assert.Equal("t", ((Dictionary<string, object?>)output["token"]!)["fn::secret"]);
    }
}
