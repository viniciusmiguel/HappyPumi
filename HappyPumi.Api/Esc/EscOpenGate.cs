#nullable enable

using System;
using System.Linq;
using System.Text.RegularExpressions;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>
/// Decides whether opening an environment is allowed. An environment is <em>gated</em> when an org approval
/// rule's pattern matches its <c>project/name</c>; a gated environment may only be opened by a principal who
/// holds an approved, unexpired grant (obtained via the open-request → approve workflow). This is the
/// enforcement point behind the approval/grant gating.
/// </summary>
public sealed class EscOpenGate(IApprovalRuleStore rules, IEscOpenRequestStore requests)
{
    /// <summary>True when an org approval rule applies to this environment.</summary>
    public bool IsGated(EnvCoordinates env)
        => MatchingRules(env).Any();

    /// <summary>True when the open should proceed: either ungated, or the requester holds an active grant.</summary>
    public bool Allows(EnvCoordinates env, string requester, DateTime nowUtc)
        => !IsGated(env) || requests.HasActiveGrant(env, requester, nowUtc);

    /// <summary>Approvals needed before a grant is issued (the strictest matching rule; at least 1).</summary>
    public int RequiredApprovals(EnvCoordinates env)
        => MatchingRules(env).Select(r => r.RequiredApprovals).DefaultIfEmpty(1).Max();

    private System.Collections.Generic.IEnumerable<Data.Entities.ApprovalRuleRow> MatchingRules(EnvCoordinates env)
    {
        var target = $"{env.Project}/{env.Name}";
        return rules.List(env.Org).Where(r => Matches(r.StackPattern, target));
    }

    // Glob match supporting '*' (any run of characters); the pattern is otherwise a literal project/name.
    private static bool Matches(string pattern, string value)
    {
        var regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(value, regex);
    }
}
