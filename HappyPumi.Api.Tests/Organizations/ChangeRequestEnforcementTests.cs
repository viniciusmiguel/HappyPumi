using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Organizations;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for change-gate enforcement (change-requests PR3) against the real Postgres-backed stores: a
/// matching approval_required gate blocks <c>Apply</c> with 400 until the required distinct approvals are
/// collected, after which the gate evaluation reports satisfied and apply commits a new revision. A gate with
/// <c>requireReapprovalOnChange</c> clears the approvals when the draft is edited, re-blocking apply.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class ChangeRequestEnforcementTests(HappyPumiApp app)
{
    private const string Project = "cr-proj";

    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task GateBlocksApplyUntilApprovalsReachThreshold()
    {
        using var creator = app.CreateAuthedClient("role:admin:alice");
        using var bob = app.CreateAuthedClient("role:admin:bob");
        using var carol = app.CreateAuthedClient("role:admin:carol");
        var org = NewOrg();
        await CreateGate(creator, org, approvals: 2, reapprove: false);
        var env = await CreateEnv(creator, org);
        var id = await CreateDraft(creator, org, env, "values:\n  region: us-west-2\n");
        await Post(creator, $"/api/change-requests/{org}/{id}/submit", new SubmitChangeRequestRequest { Description = "go" });

        using var blocked = await creator.PostAsync($"/api/change-requests/{org}/{id}/apply", JsonContent(new { }));
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);

        await Approve(bob, org, id);
        await Approve(carol, org, id);

        var cr = await GetCr(creator, org, id);
        Assert.True(cr.GateEvaluation.Satisfied);

        using var ok = await creator.PostAsync($"/api/change-requests/{org}/{id}/apply", JsonContent(new { }));
        ok.EnsureSuccessStatusCode();
        Assert.Equal("applied", (await GetCr(creator, org, id)).Status);
    }

    [Fact]
    public async Task EditingDraftClearsApprovalsWhenReapprovalRequired()
    {
        using var creator = app.CreateAuthedClient("role:admin:alice");
        using var bob = app.CreateAuthedClient("role:admin:bob");
        var org = NewOrg();
        await CreateGate(creator, org, approvals: 1, reapprove: true);
        var env = await CreateEnv(creator, org);
        var id = await CreateDraft(creator, org, env, "values:\n  region: us-west-2\n");
        await Approve(bob, org, id);
        Assert.True((await GetCr(creator, org, id)).GateEvaluation.Satisfied);

        await PatchDraft(creator, org, env, id, "values:\n  region: eu-west-1\n");

        Assert.False((await GetCr(creator, org, id)).GateEvaluation.Satisfied);
        using var blocked = await creator.PostAsync($"/api/change-requests/{org}/{id}/apply", JsonContent(new { }));
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
    }

    private static CreateChangeGateRequest GateBody(long approvals, bool reapprove) => new()
    {
        Enabled = true,
        Name = "prod-gate",
        Rule = new ChangeGateApprovalRuleInput
        {
            RuleType = "approval_required",
            NumApprovalsRequired = approvals,
            AllowSelfApproval = false,
            RequireReapprovalOnChange = reapprove,
            EligibleApprovers = new(),
        },
        Target = new ChangeGateTargetInput { EntityType = "environment", ActionTypes = new() { "update" } },
    };

    private static async Task CreateGate(HttpClient client, string org, long approvals, bool reapprove)
    {
        using var res = await client.PostAsync($"/api/change-gates/{org}",
            new StringContent(ChangeGateJson.Serialize(GateBody(approvals, reapprove)), Encoding.UTF8, "application/json"));
        res.EnsureSuccessStatusCode();
    }

    private static async Task<string> CreateEnv(HttpClient client, string org)
    {
        var name = $"env-{Guid.NewGuid():N}";
        using var res = await client.PostAsJsonAsync($"/api/esc/environments/{org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });
        res.EnsureSuccessStatusCode();
        return name;
    }

    private static async Task<string> CreateDraft(HttpClient client, string org, string env, string yaml)
    {
        using var res = await client.PostAsync($"/api/esc/environments/{org}/{Project}/{env}/drafts",
            new StringContent(yaml, Encoding.UTF8, "application/x-yaml"));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ChangeRequestRef>())!.ChangeRequestId!;
    }

    private static async Task PatchDraft(HttpClient client, string org, string env, string id, string yaml)
    {
        using var res = await client.PatchAsync($"/api/esc/environments/{org}/{Project}/{env}/drafts/{id}",
            new StringContent(yaml, Encoding.UTF8, "application/x-yaml"));
        res.EnsureSuccessStatusCode();
    }

    private static async Task Approve(HttpClient client, string org, string id)
    {
        using var res = await client.PostAsJsonAsync($"/api/change-requests/{org}/{id}/approve", new { });
        res.EnsureSuccessStatusCode();
    }

    private static async Task Post(HttpClient client, string url, object body)
    {
        using var res = await client.PostAsync(url, JsonContent(body));
        res.EnsureSuccessStatusCode();
    }

    private static StringContent JsonContent(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<GetChangeRequestResponse> GetCr(HttpClient client, string org, string id)
    {
        var raw = await client.GetStringAsync($"/api/change-requests/{org}/{id}");
        return JsonSerializer.Deserialize<GetChangeRequestResponse>(raw, ChangeGateJson.Options)!;
    }
}
