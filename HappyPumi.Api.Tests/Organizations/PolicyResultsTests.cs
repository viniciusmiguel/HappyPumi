using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for Tier 5b (ENDPOINTS.md): policy compliance results and issues. No policy evaluation
/// runs against stacks yet, so results are empty and individual issues are 404.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class PolicyResultsTests(HappyPumiApp app)
{
    [Fact]
    public async Task ComplianceResultsAreEmpty()
    {
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/orgs/acme/policyresults/compliance", new { });
        response.EnsureSuccessStatusCode();
        var results = await response.Content.ReadFromJsonAsync<GetPolicyComplianceResultsResponse>();

        Assert.Empty(results!.Rows);
    }

    [Fact]
    public async Task PolicyIssuesAreEmpty()
    {
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/orgs/acme/policyresults/issues", new { });
        response.EnsureSuccessStatusCode();
        var issues = await response.Content.ReadFromJsonAsync<ListPolicyIssuesResponse>();

        Assert.Empty(issues!.PolicyIssues);
    }

    [Fact]
    public async Task GetPolicyIssueReturns404()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/api/orgs/acme/policyresults/issues/issue-1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
