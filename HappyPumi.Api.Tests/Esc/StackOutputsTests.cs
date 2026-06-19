using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for extracting root stack outputs from a checkpoint.</summary>
public sealed class StackOutputsTests
{
    [Fact]
    public void ExtractsRootStackOutputs()
    {
        var checkpoint = new Dictionary<string, object?>
        {
            ["resources"] = new List<object?>
            {
                new Dictionary<string, object?> { ["type"] = "aws:s3/bucket:Bucket" },
                new Dictionary<string, object?>
                {
                    ["type"] = "pulumi:pulumi:Stack",
                    ["outputs"] = new Dictionary<string, object?> { ["url"] = "https://x", ["port"] = 8080 },
                },
            },
        };

        var outputs = StackOutputs.Extract(new AppUntypedDeployment { Deployment = checkpoint });

        Assert.Equal("https://x", outputs["url"]);
        Assert.Equal(8080L, outputs["port"]);
    }

    [Fact]
    public void EmptyWhenNoDeploymentOrStackResource()
    {
        Assert.Empty(StackOutputs.Extract(null));
        var noStack = new AppUntypedDeployment
        {
            Deployment = new Dictionary<string, object?> { ["resources"] = new List<object?>() },
        };
        Assert.Empty(StackOutputs.Extract(noStack));
    }
}
