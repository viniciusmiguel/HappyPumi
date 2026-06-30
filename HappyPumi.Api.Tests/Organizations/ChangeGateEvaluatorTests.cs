using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Organizations;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Unit tests for <see cref="ChangeGateEvaluator"/> (change-requests PR3) against an in-memory gate store: no
/// gates is vacuously satisfied; an approval_required gate is satisfied only once enough distinct eligible
/// approvers (the creator excluded unless self-approval is allowed) sign off; gates apply only when their
/// action set and qualified-name glob match the change request's target.
/// </summary>
public sealed class ChangeGateEvaluatorTests
{
    private const string Org = "acme";
    private const string Creator = "alice";

    [Fact]
    public void NoGatesIsSatisfiedAndAllowed()
    {
        var (eval, allowed) = Evaluate(Cr(), gates: System.Array.Empty<StoredChangeGate>());

        Assert.True(eval.Satisfied);
        Assert.True(allowed);
        Assert.Empty(eval.ApplicableGates);
    }

    [Fact]
    public void MatchingGateWithNoApproversIsUnsatisfied()
    {
        var (eval, allowed) = Evaluate(Cr(), Gate(approvals: 1));

        Assert.False(eval.Satisfied);
        Assert.False(allowed);
        var rule = (ChangeGateApprovalRuleEvaluation)eval.ApplicableGates.Single().RuleDetails;
        Assert.Equal(1, rule.RequiredApprovals);
        Assert.Empty(rule.Approvers);
    }

    [Fact]
    public void EnoughDistinctNonCreatorApproversSatisfies()
    {
        var cr = Cr(approvers: new() { "bob", "carol", "bob" });
        var (eval, allowed) = Evaluate(cr, Gate(approvals: 2));

        Assert.True(eval.Satisfied);
        Assert.True(allowed);
        var rule = (ChangeGateApprovalRuleEvaluation)eval.ApplicableGates.Single().RuleDetails;
        Assert.Equal(2, rule.Approvers.Count); // distinct: bob, carol
    }

    [Fact]
    public void CreatorApprovalIgnoredUnlessSelfApprovalAllowed()
    {
        var cr = Cr(approvers: new() { Creator });

        Assert.False(Evaluate(cr, Gate(approvals: 1, allowSelf: false)).Eval.Satisfied);
        Assert.True(Evaluate(cr, Gate(approvals: 1, allowSelf: true)).Eval.Satisfied);
    }

    [Fact]
    public void GateThatDoesNotMatchActionIsNotApplicable()
    {
        var gate = Gate(approvals: 1);
        gate.ActionTypes = new() { "delete" };

        var (eval, allowed) = Evaluate(Cr(), gate);

        Assert.True(eval.Satisfied);
        Assert.True(allowed);
        Assert.Empty(eval.ApplicableGates);
    }

    [Fact]
    public void QualifiedNameGlobScopesApplicability()
    {
        var matching = Gate(approvals: 1);
        matching.QualifiedName = "proj/*";
        var other = Gate(approvals: 1);
        other.QualifiedName = "other/*";

        var eval = Evaluate(Cr(), matching, other).Eval;

        Assert.Single(eval.ApplicableGates);
        Assert.Equal(matching.Id, eval.ApplicableGates.Single().Id);
    }

    [Fact]
    public void DisabledGateIsNotApplicable()
    {
        var gate = Gate(approvals: 1);
        gate.Enabled = false;

        Assert.Empty(Evaluate(Cr(), gate).Eval.ApplicableGates);
    }

    [Fact]
    public void RequiresReapprovalReflectsApplicableGateFlag()
    {
        var gate = Gate(approvals: 1);
        gate.RequireReapprovalOnChange = true;
        var evaluator = EvaluatorWith(gate);

        Assert.True(evaluator.RequiresReapproval(Cr()));
    }

    private static (ChangeRequestGateEvaluation Eval, bool ApplyAllowed) Evaluate(
        StoredChangeRequest cr, params StoredChangeGate[] gates)
        => EvaluatorWith(gates).Evaluate(cr);

    private static (ChangeRequestGateEvaluation Eval, bool ApplyAllowed) Evaluate(
        StoredChangeRequest cr, IEnumerable<StoredChangeGate> gates)
        => EvaluatorWith(gates.ToArray()).Evaluate(cr);

    private static ChangeGateEvaluator EvaluatorWith(params StoredChangeGate[] gates)
    {
        var store = new InMemoryChangeGateStore();
        foreach (var g in gates)
            store.Create(g);
        return new ChangeGateEvaluator(store);
    }

    private static StoredChangeRequest Cr(List<string>? approvers = null) => new()
    {
        Id = "cr-1", Org = Org, Action = "update", TargetProject = "proj", TargetEnv = "env",
        CreatedBy = Creator, Approvers = approvers ?? new(),
    };

    private static StoredChangeGate Gate(long approvals, bool allowSelf = false) => new()
    {
        Id = $"gate-{System.Guid.NewGuid():N}", Org = Org, Name = "prod-gate", Enabled = true,
        RuleType = "approval_required", NumApprovalsRequired = approvals, AllowSelfApproval = allowSelf,
        TargetEntityType = "environment", ActionTypes = new() { "update" },
    };
}
