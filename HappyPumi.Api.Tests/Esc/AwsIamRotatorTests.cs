using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Rotators.AwsIam;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the fn::rotate::aws-iam rotator (against a fake IAM client).</summary>
public sealed class AwsIamRotatorTests
{
    private static Dictionary<string, object?> Inputs(params (string, object?)[] pairs)
        => pairs.ToDictionary(p => p.Item1, p => p.Item2);

    [Fact]
    public async Task RotatesAndRetiresPreviousKey()
    {
        var client = new FakeAwsIamClient { NextKey = new("AKIA-NEW", "secret-new") };
        var rotator = new AwsIamRotator(client);
        var current = new Dictionary<string, object?> { ["accessKeyId"] = "AKIA-OLD" };

        var output = (Dictionary<string, object?>)(await rotator.RotateAsync(
            Inputs(("region", "us-east-1"), ("user", "bot")), current, CancellationToken.None))!;

        Assert.Equal("AKIA-NEW", ((Dictionary<string, object?>)output["accessKeyId"]!)["fn::secret"]);
        Assert.Equal("secret-new", ((Dictionary<string, object?>)output["secretAccessKey"]!)["fn::secret"]);
        Assert.Equal("bot", client.CreatedFor.Single());
        Assert.Equal("AKIA-OLD", client.Deleted.Single()); // previous key retired
    }

    [Fact]
    public async Task FirstRotationDeletesNothing()
    {
        var client = new FakeAwsIamClient();
        var rotator = new AwsIamRotator(client);

        await rotator.RotateAsync(Inputs(("region", "us-east-1"), ("user", "bot")), currentState: null, CancellationToken.None);

        Assert.Empty(client.Deleted);
    }

    [Fact]
    public async Task UserArnSuffixIsUsedAsUserName()
    {
        var client = new FakeAwsIamClient();
        var rotator = new AwsIamRotator(client);

        await rotator.RotateAsync(
            Inputs(("region", "us-east-1"), ("userArn", "arn:aws:iam::123456789012:user/bot")), null, CancellationToken.None);

        Assert.Equal("bot", client.CreatedFor.Single());
    }

    [Fact]
    public async Task MissingUserThrows()
    {
        var rotator = new AwsIamRotator(new FakeAwsIamClient());
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => rotator.RotateAsync(Inputs(("region", "us-east-1")), null, CancellationToken.None));
        Assert.Contains("user", ex.Message);
    }
}
