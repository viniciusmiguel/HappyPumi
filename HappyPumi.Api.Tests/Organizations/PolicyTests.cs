using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for Tier 5a (ENDPOINTS.md): policy groups CRUD, policy-pack publish/list/get/delete,
/// the config-schema endpoint, and the stack policy-packs listing. Unique org per test (shared store).
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class PolicyTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task PolicyGroupCreateListGetDelete()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        using var create = await client.PostAsJsonAsync($"/api/orgs/{org}/policygroups",
            new Dictionary<string, string> { ["name"] = "prod" });
        Assert.Equal(HttpStatusCode.NoContent, create.StatusCode);

        using var dup = await client.PostAsJsonAsync($"/api/orgs/{org}/policygroups",
            new Dictionary<string, string> { ["name"] = "prod" });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        var list = await client.GetFromJsonAsync<AppListPolicyGroupsResponse>($"/api/orgs/{org}/policygroups");
        Assert.Contains(list!.PolicyGroups, g => g.Name == "prod");

        var group = await client.GetFromJsonAsync<PolicyGroup>($"/api/orgs/{org}/policygroups/prod");
        Assert.Equal("prod", group!.Name);

        using var delete = await client.DeleteAsync($"/api/orgs/{org}/policygroups/prod");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task PolicyPackPublishListGetDelete()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        var created = await Post<AppCreatePolicyPackResponse>(client, $"/api/orgs/{org}/policypacks",
            new AppCreatePolicyPackRequest { Name = "sec", DisplayName = "Security", Policies = new List<AppPolicy>() });
        Assert.Equal(1, created.Version);
        Assert.False(string.IsNullOrEmpty(created.UploadUri));

        using var complete = await client.PostAsJsonAsync($"/api/orgs/{org}/policypacks/sec/versions/1/complete", new { });
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);

        var pack = await client.GetFromJsonAsync<AppGetPolicyPackResponse>($"/api/orgs/{org}/policypacks/sec/versions/1");
        Assert.Equal("sec", pack!.Name);
        Assert.Equal(1, pack.Version);

        var list = await client.GetFromJsonAsync<AppListPolicyPacksResponse>($"/api/orgs/{org}/policypacks");
        Assert.Contains(list!.PolicyPacks, p => p.Name == "sec");

        using var delVersion = await client.DeleteAsync($"/api/orgs/{org}/policypacks/sec/versions/1");
        Assert.Equal(HttpStatusCode.NoContent, delVersion.StatusCode);
    }

    [Fact]
    public async Task PolicyPackConfigSchemaIsEmpty()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        await Post<AppCreatePolicyPackResponse>(client, $"/api/orgs/{org}/policypacks",
            new AppCreatePolicyPackRequest { Name = "sec", DisplayName = "Security", Policies = new List<AppPolicy>() });

        var schema = await client.GetFromJsonAsync<AppGetPolicyPackConfigSchemaResponse>(
            $"/api/orgs/{org}/policypacks/sec/versions/1/schema");

        Assert.NotNull(schema);
        Assert.Empty(schema!.ConfigSchema!);
    }

    [Fact]
    public async Task StackPolicyPacksAreEmpty()
    {
        using var client = app.CreateClient();

        var response = await client.GetFromJsonAsync<AppGetStackPolicyPacksResponse>(
            "/api/stacks/happypumi/webapp/dev/policypacks");

        Assert.NotNull(response);
        Assert.Empty(response!.RequiredPolicies!);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
