#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Translates between the polymorphic change-gate wire contracts and the flat <see cref="StoredChangeGate"/>
/// persistence shape (change-requests PR1). Only the <c>approval_required</c> rule and the <c>environment</c>
/// target discriminators exist in the generated contracts, so those are the only ones mapped.
/// </summary>
internal static class ChangeGateMapper
{
    /// <summary>Reads a create/update request's fields into a stored gate (mutates in place).</summary>
    public static void ApplyInput(StoredChangeGate gate, bool enabled, string name,
        ChangeGateRuleInput? rule, ChangeGateTargetInput? target)
    {
        gate.Enabled = enabled;
        gate.Name = name;
        RuleFrom(gate, rule);
        TargetFrom(gate, target);
    }

    /// <summary>Maps a stored gate to its <see cref="ChangeGate"/> output contract.</summary>
    public static ChangeGate ToContract(StoredChangeGate g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Enabled = g.Enabled,
        Rule = RuleOf(g),
        Target = TargetOf(g),
    };

    private static void RuleFrom(StoredChangeGate gate, ChangeGateRuleInput? rule)
    {
        // The discriminator property is dropped during binding (see ChangeGateJson), so derive the rule type
        // from the concrete contract type rather than the (now-unset) RuleType property.
        if (rule is not ChangeGateApprovalRuleInput approval)
            return;
        gate.RuleType = "approval_required";
        gate.NumApprovalsRequired = approval.NumApprovalsRequired;
        gate.AllowSelfApproval = approval.AllowSelfApproval;
        gate.RequireReapprovalOnChange = approval.RequireReapprovalOnChange;
        gate.EligibleApprovers = (approval.EligibleApprovers ?? new()).Select(EligibilityFrom).ToList();
    }

    private static void TargetFrom(StoredChangeGate gate, ChangeGateTargetInput? target)
    {
        if (target is null)
            return;
        gate.TargetEntityType = target.EntityType;
        gate.ActionTypes = target.ActionTypes ?? new();
        gate.QualifiedName = target.QualifiedName;
    }

    private static EligibleApprover EligibilityFrom(ApprovalRuleEligibilityInput e) => e switch
    {
        ApprovalRuleEligibilityInputUser u => new() { EligibilityType = "specific_user", UserLogin = u.UserLogin },
        ApprovalRuleEligibilityInputTeam t => new() { EligibilityType = "team_member", TeamName = t.TeamName },
        ApprovalRuleEligibilityInputPermission p => new() { EligibilityType = "has_permission_on_target", Permission = p.Permission },
        _ => new() { EligibilityType = e.EligibilityType },
    };

    private static ChangeGateRuleOutput RuleOf(StoredChangeGate g) => new ChangeGateApprovalRuleOutput
    {
        RuleType = g.RuleType,
        NumApprovalsRequired = g.NumApprovalsRequired,
        AllowSelfApproval = g.AllowSelfApproval,
        RequireReapprovalOnChange = g.RequireReapprovalOnChange,
        EligibleApprovers = g.EligibleApprovers.Select(EligibilityToContract).ToList(),
    };

    private static ChangeGateTargetOutput TargetOf(StoredChangeGate g) => new()
    {
        ActionTypes = g.ActionTypes.ToList(),
        EntityType = g.TargetEntityType,
        QualifiedName = g.QualifiedName,
        EntityInfo = EntityInfoOf(g),
    };

    private static TargetEntity? EntityInfoOf(StoredChangeGate g)
    {
        if (g.TargetEntityType != "environment" || string.IsNullOrWhiteSpace(g.QualifiedName))
            return null;
        var parts = g.QualifiedName!.Split('/', 2);
        return new TargetEntityEnvironment { Project = parts[0], Name = parts.Length > 1 ? parts[1] : "" };
    }

    private static ApprovalRuleEligibilityOutput EligibilityToContract(EligibleApprover e) => e.EligibilityType switch
    {
        "specific_user" => new ApprovalRuleEligibilityOutputUser
        {
            EligibilityType = e.EligibilityType,
            User = new UserInfo { GithubLogin = e.UserLogin ?? "", Name = e.UserLogin ?? "", AvatarUrl = "" },
        },
        "team_member" => new ApprovalRuleEligibilityOutputTeam
        {
            EligibilityType = e.EligibilityType, Name = e.TeamName ?? "", DisplayName = e.TeamName ?? "",
        },
        "has_permission_on_target" => new ApprovalRuleEligibilityOutputPermission
        {
            EligibilityType = e.EligibilityType, Permission = e.Permission ?? "",
        },
        _ => new ApprovalRuleEligibilityOutput { EligibilityType = e.EligibilityType },
    };
}
