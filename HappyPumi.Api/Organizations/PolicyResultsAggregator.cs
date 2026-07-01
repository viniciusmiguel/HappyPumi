#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Computes the CrossGuard "policy results" views (policy-results PR2) by aggregating the recorded policy
/// findings (<see cref="IPolicyFindingStore"/>) — the violations the engine streamed during updates. Policy
/// totals fold in the known packs (<see cref="IPolicyStore"/>) so "with issues vs total" is meaningful even
/// for policies that never fired. Injected as a scoped service so the endpoints stay thin.
/// </summary>
public sealed class PolicyResultsAggregator(IPolicyFindingStore findings, IPolicyStore packs)
{
    // CSV column order for the issues export; kept next to the row projection so they can't drift apart.
    private static readonly string[] CsvHeader =
    {
        "policyName", "level", "policyPack", "stackName", "projectName",
        "resourceName", "resourceType", "message", "observedAt",
    };

    /// <summary>
    /// High-level counts. Definitions: <c>policyWithIssuesCount</c> = distinct policy names that produced a
    /// finding; <c>resourcesWithIssuesCount</c> = distinct resource URNs with a finding; <c>policyTotalCount</c>
    /// = policies across the latest version of every known pack (falls back to with-issues when no packs
    /// exist); <c>resourcesTotalCount</c> = distinct resource URNs seen (we only know resources that appear in
    /// findings, so this equals resources-with-issues).
    /// </summary>
    public PolicyResultsMetadata Metadata(string org)
    {
        var all = findings.List(org);
        var policyWithIssues = all.Select(f => f.PolicyName).Distinct().Count();
        var resourcesWithIssues = all.Select(f => f.ResourceUrn).Distinct().Count();
        var policyTotal = TotalKnownPolicies(org);
        return new PolicyResultsMetadata
        {
            PolicyWithIssuesCount = policyWithIssues,
            ResourcesWithIssuesCount = resourcesWithIssues,
            PolicyTotalCount = policyTotal > 0 ? policyTotal : policyWithIssues,
            ResourcesTotalCount = resourcesWithIssues,
        };
    }

    /// <summary>Distinct values (with counts) of a single finding field, for the filter-dropdown UI. Unknown
    /// field names yield an empty value set.</summary>
    public PolicyIssueFiltersResponse FilterValues(string org, string field)
    {
        var selector = FieldSelector(field);
        var values = selector is null
            ? new List<PolicyIssueFilterValue>()
            : DistinctCounts(findings.List(org), selector);
        return new PolicyIssueFiltersResponse { Field = field, Values = values };
    }

    /// <summary>Per-policy compliance rollup: one row per policy name that produced findings, paged by the
    /// grid's start/end row when both are supplied.</summary>
    public ListPoliciesComplianceResponse Compliance(string org, long? startRow, long? endRow)
    {
        var rows = findings.List(org)
            .GroupBy(f => f.PolicyName)
            .Select(ToComplianceRow)
            .OrderBy(r => r.PolicyName)
            .ToList();
        return new ListPoliciesComplianceResponse
        {
            Policies = Page(rows, startRow, endRow),
            TotalCount = rows.Count,
        };
    }

    /// <summary>The issues as CSV: one header row plus one row per finding (paged like <see cref="Compliance"/>).</summary>
    public string Csv(string org, long? startRow, long? endRow)
    {
        var rows = Page(findings.List(org), startRow, endRow);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", CsvHeader));
        foreach (var f in rows)
            sb.AppendLine(string.Join(",", CsvRow(f).Select(Escape)));
        return sb.ToString();
    }

    private long TotalKnownPolicies(string org)
        => packs.ListPacks(org).Sum(p => LatestPolicies(p).Count);

    private static List<AppPolicy> LatestPolicies(StoredPolicyPack pack)
    {
        if (pack.Versions.Count == 0)
            return new List<AppPolicy>();
        var latest = pack.Versions[pack.Versions.Keys.Max()];
        return latest.Policies ?? new List<AppPolicy>();
    }

    private static PolicyComplianceRow ToComplianceRow(IGrouping<string, PolicyViolationV2> g)
    {
        var first = g.First();
        var failing = g.Select(f => f.ResourceUrn).Distinct().Count();
        return new PolicyComplianceRow
        {
            PolicyName = g.Key,
            PolicyPack = first.PolicyPack,
            Severity = first.Level,
            FailingResources = failing,
            GovernedResources = failing, // we only observe resources that failed → all governed ones failed
            PercentCompliant = 0,
            PolicyGroupName = string.Empty,
            PolicyGroupType = string.Empty,
        };
    }

    private static List<PolicyIssueFilterValue> DistinctCounts(
        IReadOnlyList<PolicyViolationV2> all, Func<PolicyViolationV2, string?> selector)
        => all.Select(selector)
            .Where(v => !string.IsNullOrEmpty(v))
            .GroupBy(v => v!)
            .Select(g => new PolicyIssueFilterValue { Name = g.Key, Count = g.Count() })
            .OrderBy(v => v.Name)
            .ToList();

    private static Func<PolicyViolationV2, string?>? FieldSelector(string field) => field switch
    {
        "policyName" => f => f.PolicyName,
        "level" => f => f.Level,
        "policyPack" => f => f.PolicyPack,
        "stackName" => f => f.StackName,
        "projectName" => f => f.ProjectName,
        "resourceType" => f => f.ResourceType,
        _ => null,
    };

    private static IEnumerable<string> CsvRow(PolicyViolationV2 f) => new[]
    {
        f.PolicyName, f.Level, f.PolicyPack, f.StackName ?? "", f.ProjectName,
        f.ResourceName, f.ResourceType, f.Message, f.ObservedAt.ToString("o"),
    };

    // Quote a field only when it contains a comma, quote, or newline; embedded quotes are doubled (RFC 4180).
    private static string Escape(string field)
        => field.Contains(',') || field.Contains('"') || field.Contains('\n')
            ? $"\"{field.Replace("\"", "\"\"")}\""
            : field;

    private static List<T> Page<T>(IReadOnlyList<T> rows, long? startRow, long? endRow)
    {
        if (startRow is null || endRow is null)
            return rows.ToList();
        var skip = (int)Math.Max(0, startRow.Value);
        var take = (int)Math.Max(0, endRow.Value - startRow.Value);
        return rows.Skip(skip).Take(take).ToList();
    }
}
