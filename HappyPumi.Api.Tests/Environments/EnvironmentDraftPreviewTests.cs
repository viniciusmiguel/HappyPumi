using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Organizations;
using HappyPumi.Api.Tests;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>
/// Component tests for the ESC draft-preview endpoints (change-requests PR3) against the real Postgres stores.
/// The <c>/api/preview/esc/.../drafts</c> aliases behave exactly like their implemented <c>/api/esc/</c>
/// siblings: create → read → update → open round-trips the draft YAML, and creating a preview draft registers
/// the wrapping change request so it appears in <c>/api/change-requests/{org}</c>.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EnvironmentDraftPreviewTests(HappyPumiApp app)
{
    private const string Project = "preview-proj";

    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task PreviewDraftRoundTripsAndRegistersChangeRequest()
    {
        using var client = app.CreateAuthedClient("role:admin:alice");
        var org = NewOrg();
        var env = await CreateEnv(client, org);
        var basePath = $"/api/preview/esc/environments/{org}/{Project}/{env}/drafts";

        var id = await CreateDraft(client, basePath, "values:\n  region: us-west-2\n");
        Assert.False(string.IsNullOrEmpty(id));

        // CreatePreview registers a change request keyed by the draft id.
        var list = await GetList(client, $"/api/change-requests/{org}");
        Assert.Contains(list.ChangeRequests, c => c.Id == id);

        Assert.Contains("us-west-2", await client.GetStringAsync($"{basePath}/{id}"));

        using var patched = await client.PatchAsync($"{basePath}/{id}",
            new StringContent("values:\n  region: eu-west-1\n", Encoding.UTF8, "application/x-yaml"));
        patched.EnsureSuccessStatusCode();
        Assert.Contains("eu-west-1", await client.GetStringAsync($"{basePath}/{id}"));

        using var open = await client.PostAsync($"{basePath}/{id}/open?duration=2h", JsonContent(new { }));
        open.EnsureSuccessStatusCode();
        var session = await open.Content.ReadFromJsonAsync<OpenEnvironmentResponse>();
        Assert.False(string.IsNullOrEmpty(session!.Id));
    }

    private static async Task<string> CreateEnv(HttpClient client, string org)
    {
        var name = $"env-{Guid.NewGuid():N}";
        using var res = await client.PostAsJsonAsync($"/api/esc/environments/{org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });
        res.EnsureSuccessStatusCode();
        return name;
    }

    private static async Task<string> CreateDraft(HttpClient client, string basePath, string yaml)
    {
        using var res = await client.PostAsync(basePath, new StringContent(yaml, Encoding.UTF8, "application/x-yaml"));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ChangeRequestRef>())!.ChangeRequestId!;
    }

    private static async Task<ListChangeRequestsResponse> GetList(HttpClient client, string url)
        => JsonSerializer.Deserialize<ListChangeRequestsResponse>(await client.GetStringAsync(url), ChangeGateJson.Options)!;

    private static StringContent JsonContent(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
}
