using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Organizations;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Unit tests for <see cref="PolicyResultsAggregator"/> over seeded in-memory stores (policy-results PR2):
/// metadata counts distinct policies/resources with issues (and folds in known packs for the total); filters
/// return distinct field values with counts; compliance groups by policy; and the CSV export has a header
/// plus one row per finding.
/// </summary>
public sealed class PolicyResultsAggregatorTests
{
    private const string Org = "acme";

    private static PolicyViolationV2 Finding(string policy, string pack, string urn, string level = "advisory")
        => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Kind = "update",
            Level = level,
            Message = "boom",
            ObservedAt = DateTime.UtcNow,
            PolicyName = policy,
            PolicyPack = pack,
            PolicyPackTag = "1.0.0",
            ProjectName = "webapp",
            StackName = "dev",
            ResourceUrn = urn,
            ResourceType = "aws:s3/bucket:Bucket",
            ResourceName = urn.Split("::").Last(),
        };

    private static (PolicyResultsAggregator Agg, InMemoryPolicyFindingStore Findings, InMemoryPolicyStore Packs) Seed()
    {
        var findings = new InMemoryPolicyFindingStore();
        var packs = new InMemoryPolicyStore();
        return (new PolicyResultsAggregator(findings, packs), findings, packs);
    }

    [Fact]
    public void MetadataCountsDistinctPoliciesAndResources()
    {
        var (agg, findings, _) = Seed();
        findings.Record(Org, Finding("p1", "sec", "urn::a"));
        findings.Record(Org, Finding("p1", "sec", "urn::b")); // same policy, new resource
        findings.Record(Org, Finding("p2", "sec", "urn::a")); // new policy, same resource

        var meta = agg.Metadata(Org);

        Assert.Equal(2, meta.PolicyWithIssuesCount);
        Assert.Equal(2, meta.ResourcesWithIssuesCount);
        Assert.Equal(2, meta.PolicyTotalCount);       // no packs known → falls back to with-issues
        Assert.Equal(2, meta.ResourcesTotalCount);
    }

    [Fact]
    public void MetadataPolicyTotalFoldsInKnownPacks()
    {
        var (agg, findings, packs) = Seed();
        findings.Record(Org, Finding("p1", "sec", "urn::a"));
        packs.CreatePackVersion(Org, "sec", "Security",
            new List<AppPolicy> { new() { Name = "p1" }, new() { Name = "p2" }, new() { Name = "p3" } });

        var meta = agg.Metadata(Org);

        Assert.Equal(1, meta.PolicyWithIssuesCount);
        Assert.Equal(3, meta.PolicyTotalCount); // three policies in the pack's latest version
    }

    [Fact]
    public void FilterValuesReturnDistinctFieldValuesWithCounts()
    {
        var (agg, findings, _) = Seed();
        findings.Record(Org, Finding("p1", "sec", "urn::a", level: "mandatory"));
        findings.Record(Org, Finding("p2", "sec", "urn::b", level: "advisory"));
        findings.Record(Org, Finding("p3", "sec", "urn::c", level: "advisory"));

        var response = agg.FilterValues(Org, "level");

        Assert.Equal("level", response.Field);
        Assert.Equal(2, response.Values.Count);
        Assert.Equal(2, response.Values.Single(v => v.Name == "advisory").Count);
        Assert.Equal(1, response.Values.Single(v => v.Name == "mandatory").Count);
    }

    [Fact]
    public void FilterValuesForUnknownFieldAreEmpty()
    {
        var (agg, findings, _) = Seed();
        findings.Record(Org, Finding("p1", "sec", "urn::a"));

        Assert.Empty(agg.FilterValues(Org, "notAField").Values);
    }

    [Fact]
    public void ComplianceGroupsByPolicyWithResourceCounts()
    {
        var (agg, findings, _) = Seed();
        findings.Record(Org, Finding("p1", "sec", "urn::a"));
        findings.Record(Org, Finding("p1", "sec", "urn::b"));
        findings.Record(Org, Finding("p2", "sec", "urn::a"));

        var result = agg.Compliance(Org, startRow: null, endRow: null);

        Assert.Equal(2, result.TotalCount);
        var p1 = result.Policies.Single(r => r.PolicyName == "p1");
        Assert.Equal("sec", p1.PolicyPack);
        Assert.Equal(2, p1.FailingResources);
    }

    [Fact]
    public void CompliancePagingHonorsStartAndEndRow()
    {
        var (agg, findings, _) = Seed();
        for (var i = 0; i < 5; i++)
            findings.Record(Org, Finding($"p{i}", "sec", $"urn::{i}"));

        var page = agg.Compliance(Org, startRow: 1, endRow: 3);

        Assert.Equal(5, page.TotalCount);   // total is the full set
        Assert.Equal(2, page.Policies.Count); // page is rows [1,3)
    }

    [Fact]
    public void CsvHasHeaderAndOneRowPerFinding()
    {
        var (agg, findings, _) = Seed();
        findings.Record(Org, Finding("p1", "sec", "urn::a"));
        findings.Record(Org, Finding("p2", "sec", "urn::b"));

        var csv = agg.Csv(Org, startRow: null, endRow: null);
        var lines = csv.TrimEnd().Split('\n');

        Assert.StartsWith("policyName,level,policyPack", lines[0]);
        Assert.Equal(3, lines.Length); // header + 2 findings
    }

    [Fact]
    public void CsvEscapesCommasAndQuotes()
    {
        var (agg, findings, _) = Seed();
        var f = Finding("p1", "sec", "urn::a");
        f.Message = "has, comma and \"quote\"";
        findings.Record(Org, f);

        var csv = agg.Csv(Org, startRow: null, endRow: null);

        Assert.Contains("\"has, comma and \"\"quote\"\"\"", csv);
    }
}
