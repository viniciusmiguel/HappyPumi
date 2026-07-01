using System;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the agent-pool mutation endpoints (org-admin PR3) against the real Postgres-backed
/// <c>IAgentPoolStore</c>: create a pool via the existing create endpoint, PATCH renames it (GET reflects the
/// new name), then DELETE removes it (204) and a follow-up GET is 404. Unique org per test for isolation.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class AgentPoolMutationTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task PatchRenamesThenDeleteRemoves()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var poolId = await CreatePool(client, org, "runners", "self-hosted");

        using var patched = await client.PatchAsJsonAsync(
            $"/api/orgs/{org}/agent-pools/{poolId}", new { name = "renamed" });
        patched.EnsureSuccessStatusCode();
        var response = await patched.Content.ReadFromJsonAsync<PatchOrgAgentPoolResponse>();
        Assert.Equal("renamed", response!.Name);
        Assert.Equal("self-hosted", response.Description); // omitted description left unchanged

        var reread = await client.GetFromJsonAsync<AgentPoolDetail>($"/api/orgs/{org}/agent-pools/{poolId}");
        Assert.Equal("renamed", reread!.Name);

        using var deleted = await client.DeleteAsync($"/api/orgs/{org}/agent-pools/{poolId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var gone = await client.GetAsync($"/api/orgs/{org}/agent-pools/{poolId}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task PatchAndDeleteMissingPoolAre404()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        using var patched = await client.PatchAsJsonAsync(
            $"/api/orgs/{org}/agent-pools/ghost", new { name = "x" });
        Assert.Equal(HttpStatusCode.NotFound, patched.StatusCode);

        using var deleted = await client.DeleteAsync($"/api/orgs/{org}/agent-pools/ghost");
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
    }

    private static async Task<string> CreatePool(HttpClient client, string org, string name, string description)
    {
        using var created = await client.PostAsJsonAsync($"/api/orgs/{org}/agent-pools", new { name, description });
        created.EnsureSuccessStatusCode();
        var body = await created.Content.ReadFromJsonAsync<CreateAccessTokenResponse>();
        return body!.Id;
    }
}
