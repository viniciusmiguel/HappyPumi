using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the Settings-cluster PR4 customer-managed-key (BYOK) endpoints under
/// /api/orgs/{orgName}/cmk. They run against the real Postgres-backed CMK store and drive the full
/// lifecycle: create (becomes default) → list, set-default, disable, disable-all, and the KEK migration
/// list/retry. Unique org per test so they stay independent.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class OrganizationKeyTests(HappyPumiApp app)
{
    private static string NewOrg() => "cmk-" + Guid.NewGuid().ToString("N");

    private static object Body(string name) => new
    {
        name,
        keyType = "aws-kms",
        awsKms = new { keyArn = "arn:aws:kms:us-east-1:111:key/abc", roleArn = "arn:aws:iam::111:role/cmk" },
    };

    [Fact]
    public async Task CreateThenListRoundTripIsDefault()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        var created = await CreateOk(client, org, "primary");
        Assert.False(string.IsNullOrWhiteSpace(created.Id));
        Assert.Equal("default", created.State);
        Assert.Equal("arn:aws:kms:us-east-1:111:key/abc", created.AwsKms!.KeyArn);

        var list = await client.GetFromJsonAsync<List<CustomerManagedKey>>($"/api/orgs/{org}/cmk");
        var only = Assert.Single(list!);
        Assert.Equal(created.Id, only.Id);
        Assert.Equal("default", only.State);
    }

    [Fact]
    public async Task SetDefaultPromotesTheChosenKey()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var first = await CreateOk(client, org, "first");
        await CreateOk(client, org, "second"); // demotes first

        using var resp = await client.PostAsync($"/api/orgs/{org}/cmk/{first.Id}/default", EmptyJson());
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var list = await client.GetFromJsonAsync<List<CustomerManagedKey>>($"/api/orgs/{org}/cmk");
        Assert.Equal("default", list!.Single(k => k.Id == first.Id).State);
    }

    [Fact]
    public async Task SetDefaultUnknownKeyReturns404()
    {
        using var client = app.CreateClient();
        using var resp = await client.PostAsync($"/api/orgs/{NewOrg()}/cmk/{Guid.NewGuid():N}/default", EmptyJson());
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task DisableMarksKeyDisabled()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var key = await CreateOk(client, org, "primary");

        using var resp = await client.PostAsync($"/api/orgs/{org}/cmk/{key.Id}/disable", EmptyJson());
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var list = await client.GetFromJsonAsync<List<CustomerManagedKey>>($"/api/orgs/{org}/cmk");
        Assert.Equal("disabled", list!.Single().State);
    }

    [Fact]
    public async Task DisableUnknownKeyReturns404()
    {
        using var client = app.CreateClient();
        using var resp = await client.PostAsync($"/api/orgs/{NewOrg()}/cmk/{Guid.NewGuid():N}/disable", EmptyJson());
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task DisableAllRevertsEveryKey()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        await CreateOk(client, org, "a");
        await CreateOk(client, org, "b");

        using var resp = await client.PostAsync($"/api/orgs/{org}/cmk/disable", EmptyJson());
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var list = await client.GetFromJsonAsync<List<CustomerManagedKey>>($"/api/orgs/{org}/cmk");
        Assert.All(list!, k => Assert.Equal("disabled", k.State));
    }

    [Fact]
    public async Task CreateRejectsBlankName()
    {
        using var client = app.CreateClient();
        using var resp = await client.PostAsJsonAsync($"/api/orgs/{NewOrg()}/cmk",
            new { name = "", keyType = "aws-kms" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ListMigrationsThenRetry()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        await CreateOk(client, org, "primary"); // records a migration

        var migrations = await client.GetFromJsonAsync<List<KeyEncryptionKeyMigration>>($"/api/orgs/{org}/cmk/migration");
        var only = Assert.Single(migrations!);
        Assert.Equal("completed", only.State);

        using var retry = await client.PostAsync($"/api/orgs/{org}/cmk/migration/retry", EmptyJson());
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
    }

    private async Task<CustomerManagedKey> CreateOk(HttpClient client, string org, string name)
    {
        using var resp = await client.PostAsJsonAsync($"/api/orgs/{org}/cmk", Body(name));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<CustomerManagedKey>())!;
    }

    private static StringContent EmptyJson() => new("{}", System.Text.Encoding.UTF8, "application/json");
}
