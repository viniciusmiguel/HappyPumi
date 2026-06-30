#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A persisted change request (PR2, ADR-0005). Key: (Org, Id) — the id is the wrapped ESC draft id. The
/// <see cref="Approvers"/> collection is stored as jsonb; scalar columns carry the fields the list/get
/// endpoints query and sort on.
/// </summary>
public sealed class ChangeRequestRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Action { get; set; } = "update";
    public string Description { get; set; } = "";
    public string TargetProject { get; set; } = "";
    public string TargetEnv { get; set; } = "";
    public string Status { get; set; } = "draft";
    public long LatestRevisionNumber { get; set; }
    public string CreatedBy { get; set; } = "happypumi";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Distinct approver logins (jsonb).</summary>
    public List<string> Approvers { get; set; } = new();
}
