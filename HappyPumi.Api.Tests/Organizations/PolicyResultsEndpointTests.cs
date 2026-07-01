using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the policy-results endpoints (policy-results PR2) against real Postgres. Findings are
/// seeded through the engine event-batch path (the same route that records violations during an update), then
/// metadata / filters / compliance / export reflect them. Unique org per test for independence.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class PolicyResultsEndpointTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task MetadataReflectsSeededFindings()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        await SeedTwoFindings(client, org);

        var meta = await client.GetFromJsonAsync<PolicyResultsMetadata>($"/api/orgs/{org}/policyresults/metadata");

        Assert.NotNull(meta);
        Assert.Equal(2, meta!.PolicyWithIssuesCount);      // two distinct policies fired
        Assert.Equal(2, meta.ResourcesWithIssuesCount);    // two distinct resources
    }

    [Fact]
    public async Task IssueFiltersReturnDistinctFieldValues()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        await SeedTwoFindings(client, org);

        using var resp = await client.PostAsJsonAsync(
            $"/api/orgs/{org}/policyresults/issues/filters",
            new PolicyIssueFiltersRequest { Field = "policyPack" });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<PolicyIssueFiltersResponse>();

        Assert.Equal("policyPack", body!.Field);
        Assert.Contains(body.Values, v => v.Name == "widget-policy");
    }

    [Fact]
    public async Task ComplianceGroupsByPolicy()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        await SeedTwoFindings(client, org);

        using var resp = await client.PostAsJsonAsync(
            $"/api/orgs/{org}/policyresults/policies", new AngularGridGetRowsRequest());
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ListPoliciesComplianceResponse>();

        Assert.Equal(2, body!.TotalCount);
        Assert.Contains(body.Policies, p => p.PolicyName == "min-length" && p.PolicyPack == "widget-policy");
    }

    [Fact]
    public async Task ExportReturnsCsvWithHeaderAndRows()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        await SeedTwoFindings(client, org);

        using var resp = await client.PostAsJsonAsync(
            $"/api/orgs/{org}/policyresults/issues/export", new AngularGridGetRowsRequest());
        resp.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", resp.Content.Headers.ContentType!.MediaType);
        var csv = await resp.Content.ReadAsStringAsync();
        var lines = csv.TrimEnd().Split('\n');

        Assert.StartsWith("policyName,level,policyPack", lines[0]);
        Assert.Equal(3, lines.Length); // header + 2 findings
    }

    // Streams two policy-violation events (distinct policies + resources) through the update event batch so the
    // finding store records them, exactly as the engine does during a real update.
    private static async Task SeedTwoFindings(HttpClient client, string org)
    {
        var batch = new AppEngineEventBatch
        {
            Events = new List<AppEngineEvent>
            {
                PolicyEvent("min-length", "urn:pulumi:dev::widget-template::random:index/randomPet:RandomPet::a"),
                PolicyEvent("required-tags", "urn:pulumi:dev::widget-template::aws:s3/bucket:Bucket::b"),
            },
        };
        using var post = await client.PostAsJsonAsync(
            $"/api/stacks/{org}/widget-template/dev/update/u1/events/batch", batch);
        post.EnsureSuccessStatusCode();
    }

    private static AppEngineEvent PolicyEvent(string policyName, string urn) => new()
    {
        PolicyEvent = new AppPolicyEvent
        {
            PolicyName = policyName, PolicyPackName = "widget-policy",
            PolicyPackVersion = "1", PolicyPackVersionTag = "1.0.0", EnforcementLevel = "advisory",
            Message = $"{policyName} violated", ResourceUrn = urn,
        },
    };
}
