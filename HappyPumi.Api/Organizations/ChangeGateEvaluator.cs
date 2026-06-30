#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Evaluates a change request against the org's enabled change gates (change-requests PR3). A gate is
/// applicable when it targets <c>environment</c>, its action set matches the CR action, and its qualified-name
/// glob matches <c>"{project}/{env}"</c>. Each <c>approval_required</c> gate is satisfied once enough distinct
/// eligible approvers (the creator excluded unless self-approval is allowed) sign off. Apply is allowed only
/// when every applicable gate is satisfied.
/// </summary>
public sealed class ChangeGateEvaluator(IChangeGateStore gates)
{
    /// <summary>Evaluates <paramref name="cr"/>; returns the wire evaluation and whether apply may proceed.</summary>
    public (ChangeRequestGateEvaluation Eval, bool ApplyAllowed) Evaluate(StoredChangeRequest cr)
    {
        var evaluations = ApplicableGates(cr).Select(g => EvaluateGate(cr, g)).ToList();
        var satisfied = evaluations.All(e => e.Satisfied);
        var eval = new ChangeRequestGateEvaluation { ApplicableGates = evaluations, Satisfied = satisfied };
        return (eval, satisfied);
    }

    /// <summary>True when any applicable gate clears approvals on a new revision (requireReapprovalOnChange).</summary>
    public bool RequiresReapproval(StoredChangeRequest cr)
        => ApplicableGates(cr).Any(g => g.RequireReapprovalOnChange);

    private IEnumerable<StoredChangeGate> ApplicableGates(StoredChangeRequest cr)
    {
        var target = $"{cr.TargetProject}/{cr.TargetEnv}";
        return gates.List(cr.Org).Where(g =>
            g.Enabled &&
            g.TargetEntityType == "environment" &&
            (g.ActionTypes.Count == 0 || g.ActionTypes.Contains(cr.Action)) &&
            (string.IsNullOrEmpty(g.QualifiedName) || Matches(g.QualifiedName, target)));
    }

    private static ChangeGateEvaluation EvaluateGate(StoredChangeRequest cr, StoredChangeGate gate)
    {
        var approvers = EligibleApprovers(cr, gate).ToList();
        return new ChangeGateEvaluation
        {
            Id = gate.Id,
            Name = gate.Name,
            Satisfied = approvers.Count >= gate.NumApprovalsRequired,
            RuleDetails = new ChangeGateApprovalRuleEvaluation
            {
                RuleType = "approval_required",
                RequiredApprovals = gate.NumApprovalsRequired,
                Approvers = approvers.Select(ChangeRequestMapper.UserOf).ToList(),
            },
        };
    }

    // Distinct approvers, excluding the CR creator unless the gate permits self-approval (separation of duties).
    private static IEnumerable<string> EligibleApprovers(StoredChangeRequest cr, StoredChangeGate gate)
        => cr.Approvers.Distinct().Where(a => gate.AllowSelfApproval || a != cr.CreatedBy);

    // Glob match supporting '*' (any run of characters); copied from EscOpenGate to avoid a dependency on it.
    private static bool Matches(string pattern, string value)
    {
        var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value, regex);
    }
}
