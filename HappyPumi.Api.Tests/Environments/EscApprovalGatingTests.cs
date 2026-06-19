using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for approval/grant gating of OpenEnvironment (open-request → approve → grant).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscApprovalGatingTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-gating";

    [Fact]
    public async Task GatedEnvironmentRequiresAnApprovedGrantFromADistinctApprover()
    {
        // Two distinct admin identities: the requester and a separate approver.
        using var requester = app.CreateAuthedClient("role:admin:alice");
        using var approver = app.CreateAuthedClient("role:admin:bob");

        var name = await CreateEnv(requester);
        var url = $"/api/esc/environments/{Org}/{Project}/{name}";

        // Ungated: opens immediately.
        Assert.Equal(HttpStatusCode.OK, (await requester.PostAsJsonAsync($"{url}/open", new { })).StatusCode);

        // Gate this exact environment with an approval rule (scoped so other envs are unaffected).
        await CreateApprovalRule(requester, pattern: $"{Project}/{name}");
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.PostAsJsonAsync($"{url}/open", new { })).StatusCode);

        var id = await CreateOpenRequest(requester, url);

        // Separation of duties: the requester cannot approve their own request.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await requester.PostAsJsonAsync($"/api/change-requests/{Org}/{id}/approve", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.PostAsJsonAsync($"{url}/open", new { })).StatusCode);

        // A distinct approver grants access, then the requester's open succeeds.
        (await approver.PostAsJsonAsync($"/api/change-requests/{Org}/{id}/approve", new { })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await requester.PostAsJsonAsync($"{url}/open", new { })).StatusCode);

        // Withdrawing the approval revokes the grant: gated again.
        (await approver.DeleteAsync($"/api/change-requests/{Org}/{id}/approve")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.PostAsJsonAsync($"{url}/open", new { })).StatusCode);

        var audit = await approver.GetFromJsonAsync<ResponseAuditLogs>($"/api/orgs/{Org}/auditlogs");
        Assert.Contains(audit!.AuditLogEvents, e => e.Event == "changeRequest.approve" && e.Description!.Contains(name));
    }

    private static async Task<string> CreateOpenRequest(HttpClient client, string envUrl)
    {
        var created = await (await client.PostAsJsonAsync($"{envUrl}/open/request",
            new CreateEnvironmentOpenRequest { AccessDurationSeconds = 3600, GrantExpirationSeconds = 7200 }))
            .Content.ReadFromJsonAsync<CreateEnvironmentOpenRequestResponse>();
        return created!.ChangeRequests.Single().ChangeRequestId!;
    }

    private static async Task CreateApprovalRule(HttpClient client, string pattern)
    {
        using var res = await client.PostAsJsonAsync($"/api/orgs/{Org}/approval-rules",
            new { name = $"rule-{Guid.NewGuid():N}", stackPattern = pattern, requiredApprovals = 1 });
        res.EnsureSuccessStatusCode();
    }

    private static async Task<string> CreateEnv(HttpClient client)
    {
        var name = $"env-{Guid.NewGuid():N}";
        using var res = await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });
        res.EnsureSuccessStatusCode();
        return name;
    }
}
