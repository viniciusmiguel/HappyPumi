using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemoryOrgWebhookStore"/>: create/get/list/update/delete are scoped per org,
/// duplicate names are rejected (→ 409), a PATCH applies only supplied fields, and the secret is preserved
/// across updates that omit it.
/// </summary>
public sealed class InMemoryOrgWebhookStoreTests
{
    private const string Org = "acme";

    private static WebhookResponse Hook(string name = "ci", string url = "https://hooks.test/x", string? secret = null)
        => new() { Name = name, PayloadUrl = url, Active = true, OrganizationName = Org, Secret = secret };

    [Fact]
    public void CreateThenGetReturnsTheWebhook()
    {
        var store = new InMemoryOrgWebhookStore();
        Assert.NotNull(store.Create(Org, Hook()));
        Assert.Equal("ci", store.Get(Org, "ci")!.Name);
    }

    [Fact]
    public void CreateDuplicateNameReturnsNull()
    {
        var store = new InMemoryOrgWebhookStore();
        store.Create(Org, Hook());
        Assert.Null(store.Create(Org, Hook()));
    }

    [Fact]
    public void ListIsScopedPerOrg()
    {
        var store = new InMemoryOrgWebhookStore();
        store.Create(Org, Hook("a"));
        store.Create("other", Hook("b"));
        Assert.Single(store.List(Org));
        Assert.Equal("a", store.List(Org)[0].Name);
    }

    [Fact]
    public void UpdateAppliesPatchAndPreservesUnsuppliedSecret()
    {
        var store = new InMemoryOrgWebhookStore();
        store.Create(Org, Hook(secret: "s3cr3t"));

        var updated = store.Update(Org, "ci", new Webhook { Active = false, DisplayName = "CI" });

        Assert.False(updated!.Active);
        Assert.Equal("CI", updated.DisplayName);
        Assert.Equal("s3cr3t", store.Get(Org, "ci")!.Secret); // omitted secret kept
    }

    [Fact]
    public void UpdateMissingWebhookReturnsNull()
        => Assert.Null(new InMemoryOrgWebhookStore().Update(Org, "nope", new Webhook()));

    [Fact]
    public void DeleteRemovesAndReportsMissing()
    {
        var store = new InMemoryOrgWebhookStore();
        store.Create(Org, Hook());
        Assert.True(store.Delete(Org, "ci"));
        Assert.False(store.Delete(Org, "ci"));
    }
}
