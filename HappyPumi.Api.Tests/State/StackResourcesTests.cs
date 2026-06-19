using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>Unit tests for extracting the resource registry out of a stack's state checkpoint.</summary>
public sealed class StackResourcesTests
{
    [Fact]
    public void ExtractReadsResourcesFromCheckpoint()
    {
        var deployment = new AppUntypedDeployment
        {
            Version = 4,
            Deployment = new Dictionary<string, object?>
            {
                ["manifest"] = new Dictionary<string, object?>(),
                ["resources"] = new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        ["urn"] = "urn:pulumi:dev::webstore::pulumi:pulumi:Stack::webstore-dev",
                        ["type"] = "pulumi:pulumi:Stack", ["custom"] = false,
                    },
                    new()
                    {
                        ["urn"] = "urn:pulumi:dev::webstore::aws:s3/bucketV2:BucketV2::assets",
                        ["type"] = "aws:s3/bucketV2:BucketV2", ["custom"] = true,
                        ["id"] = "webstore-assets", ["provider"] = "urn:pulumi:dev::webstore::pulumi:providers:aws::default::uuid",
                    },
                },
            },
        };

        var resources = StackResources.Extract(deployment);

        Assert.Equal(2, resources.Count);
        Assert.Equal("pulumi:pulumi:Stack", resources[0].Type);
        Assert.False(resources[0].Custom);
        var bucket = resources[1];
        Assert.Equal("aws:s3/bucketV2:BucketV2", bucket.Type);
        Assert.True(bucket.Custom);
        Assert.Equal("webstore-assets", bucket.Id);
        Assert.Contains("providers:aws", bucket.Provider);
    }

    [Fact]
    public void ExtractReturnsEmptyForNullOrEmptyCheckpoint()
    {
        Assert.Empty(StackResources.Extract(null));
        Assert.Empty(StackResources.Extract(new AppUntypedDeployment { Deployment = null }));
        Assert.Empty(StackResources.Extract(new AppUntypedDeployment
        {
            Deployment = new Dictionary<string, object?> { ["manifest"] = new Dictionary<string, object?>() },
        }));
    }
}
