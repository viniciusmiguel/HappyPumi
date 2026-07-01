using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryAuditExportConfigStore"/>: an unset org yields a disabled default;
/// Upsert persists (creating the row on first write) and round-trips through Get; Delete resets to the
/// disabled default and reports true/false.
/// </summary>
public sealed class InMemoryAuditExportConfigStoreTests
{
    [Fact]
    public void GetUnsetReturnsDisabledDefault()
    {
        var store = new InMemoryAuditExportConfigStore();

        var config = store.Get("acme");

        Assert.Equal("acme", config.Org);
        Assert.False(config.Enabled);
        Assert.Null(config.S3BucketName);
    }

    [Fact]
    public void UpsertPersistsAndRoundTrips()
    {
        var store = new InMemoryAuditExportConfigStore();

        store.Upsert("acme", c =>
        {
            c.Enabled = true;
            c.S3BucketName = "audit-bucket";
            c.IamRoleArn = "arn:aws:iam::123:role/pulumi";
            c.S3PathPrefix = "logs/";
        });

        var read = store.Get("acme");
        Assert.True(read.Enabled);
        Assert.Equal("audit-bucket", read.S3BucketName);
        Assert.Equal("arn:aws:iam::123:role/pulumi", read.IamRoleArn);
        Assert.Equal("logs/", read.S3PathPrefix);
    }

    [Fact]
    public void DeleteResetsToDisabledDefault()
    {
        var store = new InMemoryAuditExportConfigStore();
        store.Upsert("acme", c => c.Enabled = true);

        Assert.True(store.Delete("acme"));
        Assert.False(store.Get("acme").Enabled);
        Assert.False(store.Delete("acme"));
    }
}
