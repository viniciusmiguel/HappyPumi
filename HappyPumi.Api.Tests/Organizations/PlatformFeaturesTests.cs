using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the platform/management surfaces wired off the audit-gap-3 placeholders: IDP services,
/// cloud accounts, VCS connections, OIDC issuers, approval rules — and the audit log they record into.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class PlatformFeaturesTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task ServiceCreateListAndConflict()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        var created = await Post<Service>(client, $"/api/orgs/{org}/services",
            new { name = "checkout", description = "Checkout stacks" });
        Assert.Equal("checkout", created.Name);

        using var dup = await client.PostAsJsonAsync($"/api/orgs/{org}/services", new { name = "checkout" });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        var list = await client.GetFromJsonAsync<ListServicesResponse>($"/api/orgs/{org}/services");
        Assert.Contains(list!.Services, s => s.Name == "checkout");
    }

    [Fact]
    public async Task CloudAccountCreateListAndAuditRecorded()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        using var create = await client.PostAsJsonAsync($"/api/orgs/{org}/cloud-accounts",
            new { name = "prod-aws", provider = "aws", description = "Production" });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        using var listResp = await client.GetAsync($"/api/orgs/{org}/cloud-accounts");
        using var doc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        var accounts = doc.RootElement.GetProperty("accounts");
        Assert.Equal(1, accounts.GetArrayLength());
        Assert.Equal("prod-aws", accounts[0].GetProperty("name").GetString());

        // The create recorded an audit event.
        var audit = await client.GetFromJsonAsync<ResponseAuditLogs>($"/api/orgs/{org}/auditlogs");
        Assert.Contains(audit!.AuditLogEvents, e => e.Event == "cloudAccount.create");
    }

    [Fact]
    public async Task ApprovalRuleCreateAndList()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        using var create = await client.PostAsJsonAsync($"/api/orgs/{org}/approval-rules",
            new { name = "prod-review", stackPattern = "*/prod", requiredApprovals = 2 });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        using var listResp = await client.GetAsync($"/api/orgs/{org}/approval-rules");
        using var doc = JsonDocument.Parse(await listResp.Content.ReadAsStringAsync());
        var rules = doc.RootElement.GetProperty("rules");
        Assert.Equal("prod-review", rules[0].GetProperty("name").GetString());
        Assert.Equal(2, rules[0].GetProperty("requiredApprovals").GetInt32());
        Assert.True(rules[0].GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task VcsConnectionAndOidcIssuerRoundTrip()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        using var vcs = await client.PostAsJsonAsync($"/api/orgs/{org}/vcs-connections",
            new { name = "acme", kind = "gitlab" });
        Assert.Equal(HttpStatusCode.OK, vcs.StatusCode);
        using var issuer = await client.PostAsJsonAsync($"/api/orgs/{org}/oidc-issuers",
            new { name = "gha", url = "https://token.actions.githubusercontent.com" });
        Assert.Equal(HttpStatusCode.OK, issuer.StatusCode);

        using var vcsList = await client.GetAsync($"/api/orgs/{org}/vcs-connections");
        Assert.Contains("gitlab", await vcsList.Content.ReadAsStringAsync());
        using var issuerList = await client.GetAsync($"/api/orgs/{org}/oidc-issuers");
        Assert.Contains("githubusercontent", await issuerList.Content.ReadAsStringAsync());
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
