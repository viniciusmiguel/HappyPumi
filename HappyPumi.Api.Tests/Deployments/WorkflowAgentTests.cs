using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Deployments;

/// <summary>
/// Component tests for the customer-managed workflow agent surface (reverse-engineered black-box from the
/// prebuilt agent; see workspace research/, ADR-0008): agent-pool token mint + validation, the deployment
/// poll/claim, job-definition, and status callbacks. These are the endpoints that let the real prebuilt
/// agent run a deployment end-to-end against HappyPumi.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class WorkflowAgentTests(HappyPumiApp app)
{
    /// <summary>Creates an agent pool and returns a client that presents its token (as the agent does).</summary>
    private async Task<HttpClient> PoolClientAsync()
    {
        using var admin = app.CreateClient();
        using var created = await admin.PostAsJsonAsync("/api/orgs/happypumi/agent-pools",
            new CreateOrgAgentPoolRequest { Name = "test", Description = "test pool" });
        created.EnsureSuccessStatusCode();
        var token = (await created.Content.ReadFromJsonAsync<CreateAccessTokenResponse>())!.TokenValue;
        Assert.False(string.IsNullOrEmpty(token));

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);
        return client;
    }

    [Fact]
    public async Task PoolScopedEndpointsRejectMissingOrInvalidToken()
    {
        using var anon = app.CreateClient();
        // No token → 401 on a pool-scoped endpoint.
        using var noTok = await anon.GetAsync("/api/background-activities/token");
        Assert.Equal(HttpStatusCode.Unauthorized, noTok.StatusCode);

        using var bad = app.CreateClient();
        bad.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", "not-a-real-pool-token");
        using var badPoll = await bad.GetAsync("/api/deployments/poll");
        Assert.Equal(HttpStatusCode.Unauthorized, badPoll.StatusCode);
    }

    [Fact]
    public async Task ConfigurationReturnsJsonArray()
    {
        using var client = await PoolClientAsync();
        // The agent unmarshals this into []BackgroundActivityConfiguration; an object would fail to parse.
        var configs = await client.GetFromJsonAsyncViaPost("/api/background-activities/configuration");
        Assert.NotNull(configs);
        Assert.NotEmpty(configs!);
        Assert.Equal("deployment", configs![0].Kind);
    }

    [Fact]
    public async Task TokenAndExecutorAndLeaseRespond()
    {
        using var client = await PoolClientAsync();

        using var token = await client.GetAsync("/api/background-activities/token");
        Assert.Equal(HttpStatusCode.OK, token.StatusCode);

        using var executor = await client.GetAsync("/api/deployments/executor");
        Assert.Equal(HttpStatusCode.OK, executor.StatusCode);

        // No insights/policy work: the lease poll is empty.
        using var lease = await client.PostAsync("/api/background-activities/worker/lease/acquire", null);
        Assert.Equal(HttpStatusCode.NoContent, lease.StatusCode);
    }

    [Fact]
    public async Task DeploymentDispatchLifecycle()
    {
        using var client = await PoolClientAsync();
        var stack = $"wf-{Guid.NewGuid():N}";
        var baseUrl = $"/api/stacks/happypumi/webapp/{stack}";

        // 1) enqueue a deployment (user action; not pool-scoped)
        using var created = await client.PostAsJsonAsync($"{baseUrl}/deployments",
            new CreateDeploymentRequest { Operation = "update" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        // 2) the agent poller claims the next queued deployment (pool-scoped → needs the pool token)
        var def = await ClaimADeployment(client);
        Assert.False(string.IsNullOrEmpty(def.JobId));
        Assert.False(string.IsNullOrEmpty(def.TypeSpecificId));

        // 3) the runner fetches the job definition — must be runnable (non-null image, a step with a command)
        var job = await client.GetFromJsonAsync<JobDefinition>($"/api/workflow/jobs/{def.JobId}");
        Assert.NotNull(job!.Image);
        Assert.False(string.IsNullOrEmpty(job.Image.Reference));
        Assert.NotEmpty(job.Steps);
        Assert.False(string.IsNullOrEmpty(job.Steps[0].Run));

        // 4) the runner reports the step succeeded — WITHOUT a Content-Type (regression: must not 415)
        using var status = await PatchNoContentType(
            client, $"/api/workflow/jobs/{def.JobId}/0/status", "{\"status\":\"succeeded\"}");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);

        // 5) the agent's final status check reflects the reported status
        using var check = await client.PostAsync(
            $"/api/agent-workflows/deployment:{def.TypeSpecificId}/check", null);
        check.EnsureSuccessStatusCode();
        var body = await check.Content.ReadAsStringAsync();
        Assert.Contains("succeeded", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PollDrainsToNoContent()
    {
        using var client = await PoolClientAsync();
        // Drain whatever is queued, then assert the empty queue returns 204.
        for (var i = 0; i < 50; i++)
        {
            using var r = await client.GetAsync("/api/deployments/poll");
            if (r.StatusCode == HttpStatusCode.NoContent)
                return;
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        Assert.Fail("poll never drained to 204 after 50 claims");
    }

    private static async Task<AgentWorkflowDefinition> ClaimADeployment(HttpClient client)
    {
        for (var i = 0; i < 50; i++)
        {
            using var r = await client.GetAsync("/api/deployments/poll");
            if (r.StatusCode == HttpStatusCode.OK)
                return (await r.Content.ReadFromJsonAsync<AgentWorkflowDefinition>())!;
            Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        }

        throw new Xunit.Sdk.XunitException("no deployment was claimable from the poll queue");
    }

    private static async Task<HttpResponseMessage> PatchNoContentType(HttpClient client, string url, string json)
    {
        using var req = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(json, Encoding.UTF8),
        };
        req.Content.Headers.ContentType = null; // mimic the runner, which sends no Content-Type
        return await client.SendAsync(req);
    }
}

internal static class WorkflowAgentTestExtensions
{
    /// <summary>POSTs (the agent's verb) and reads the JSON-array configuration response.</summary>
    public static async Task<List<BackgroundActivityConfiguration>?> GetFromJsonAsyncViaPost(
        this HttpClient client, string url)
    {
        using var r = await client.PostAsync(url, null);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<List<BackgroundActivityConfiguration>>();
    }
}
