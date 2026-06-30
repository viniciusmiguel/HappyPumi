#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// A persisted change gate (change-requests feature group, PR1, ADR-0005). A gate requires that certain
/// <see cref="ActionTypes"/> on a target entity are staged via a change request that satisfies an approval
/// rule before they can be applied. Only the <c>approval_required</c> rule and the <c>environment</c> target
/// are modelled today (the generated contracts expose only those discriminators).
/// </summary>
public sealed class StoredChangeGate
{
    public required string Id { get; init; }
    public required string Org { get; init; }
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public string RuleType { get; set; } = "approval_required";
    public long NumApprovalsRequired { get; set; } = 1;
    public bool AllowSelfApproval { get; set; }
    public bool RequireReapprovalOnChange { get; set; }
    public List<EligibleApprover> EligibleApprovers { get; set; } = new();
    public string TargetEntityType { get; set; } = "environment";
    public List<string> ActionTypes { get; set; } = new();
    public string? QualifiedName { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// One eligibility entry of an approval rule. <see cref="EligibilityType"/> selects which of the optional
/// fields is meaningful: <c>specific_user</c> → <see cref="UserLogin"/>, <c>team_member</c> →
/// <see cref="TeamName"/>, <c>has_permission_on_target</c> → <see cref="Permission"/>.
/// </summary>
public sealed class EligibleApprover
{
    public required string EligibilityType { get; set; } // specific_user | team_member | has_permission_on_target
    public string? UserLogin { get; set; }
    public string? TeamName { get; set; }
    public string? Permission { get; set; }
}

/// <summary>
/// Persistence seam for change gates (ADR-0005). Backed by PostgreSQL in production and an in-memory map in
/// unit tests. Gates are keyed by id within an org. <see cref="Update"/> mutates in place and returns the
/// updated record (null when missing), mirroring the other settings-cluster stores.
/// </summary>
public interface IChangeGateStore
{
    /// <summary>Persists a new gate and returns it.</summary>
    StoredChangeGate Create(StoredChangeGate gate);

    /// <summary>All gates for an org, newest first.</summary>
    IReadOnlyList<StoredChangeGate> List(string org);

    /// <summary>A single gate by id within an org, or null when missing.</summary>
    StoredChangeGate? Get(string org, string id);

    /// <summary>Applies <paramref name="mutate"/> to the gate and returns it; null when missing.</summary>
    StoredChangeGate? Update(string org, string id, Action<StoredChangeGate> mutate);

    /// <summary>Removes a gate. False when it does not exist.</summary>
    bool Delete(string org, string id);
}
