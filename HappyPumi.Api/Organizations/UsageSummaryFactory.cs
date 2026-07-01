#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Builds the deterministic shapes returned by the resource-search and usage-summary endpoints (PR4).
/// HappyPumi has no resource-search index or usage-metering data source, so these endpoints return empty
/// result sets / zero aggregations rather than fabricating data (design doc 2026-07-01, "real where a store
/// exists"). Centralised here so the four search endpoints and two summary endpoints don't duplicate the
/// empty-shape construction.
/// </summary>
public static class UsageSummaryFactory
{
    /// <summary>CSV header for the resource-search export — no data rows (no resource store to export).</summary>
    public const string ExportCsvHeader = "urn,type,project,stack\n";

    /// <summary>
    /// An empty resource-search result: no aggregations, no resources, total 0, and a page with no links.
    /// Used by the dashboard-aggregations and v2-search endpoints.
    /// </summary>
    public static ResourceSearchResult EmptySearchResult() => new()
    {
        Aggregations = new Dictionary<string, Aggregation>(),
        Pagination = new ResourceSearchPagination(),
        Resources = new List<ResourceResult>(),
        Total = 0,
    };

    /// <summary>A resource-count summary with no data points (no metering data source).</summary>
    public static GetResourceCountSummaryResponse EmptySummary() => new()
    {
        Summary = new List<ResourceCountSummary>(),
    };

    /// <summary>
    /// A single-point summary carrying the org's ESC environment count as the resource count. ESC secret
    /// *hours* are not metered, so <c>ResourceHours</c> is 0 — only the live environment count is real.
    /// </summary>
    public static GetResourceCountSummaryResponse EnvironmentCountSummary(long environments) => new()
    {
        Summary = new List<ResourceCountSummary>
        {
            new() { Year = DateTime.UtcNow.Year, Resources = environments, ResourceHours = 0 },
        },
    };
}
