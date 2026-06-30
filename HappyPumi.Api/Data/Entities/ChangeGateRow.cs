#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A persisted change gate (PR1, ADR-0005). Key: (Org, Id). The <see cref="EligibleApprovers"/> and
/// <see cref="ActionTypes"/> collections are stored as jsonb; scalar columns carry the fields the list/read
/// endpoints query and sort on.
/// </summary>
public sealed class ChangeGateRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public string RuleType { get; set; } = "approval_required";
    public long NumApprovalsRequired { get; set; } = 1;
    public bool AllowSelfApproval { get; set; }
    public bool RequireReapprovalOnChange { get; set; }

    /// <summary>Eligible-approver entries (jsonb).</summary>
    public List<EligibleApprover> EligibleApprovers { get; set; } = new();

    public string TargetEntityType { get; set; } = "environment";

    /// <summary>Targeted action types, e.g. "update" (jsonb).</summary>
    public List<string> ActionTypes { get; set; } = new();

    public string? QualifiedName { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
