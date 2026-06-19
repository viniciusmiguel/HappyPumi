#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for policy findings — the policy violations the engine reports through an update's
/// event stream (AppPolicyEvent). They surface on the console's "Policy findings" page and the
/// <c>/policyresults/violationsv2</c> endpoint. In-memory by default (ADR-0005); safe for concurrent use.
/// </summary>
public interface IPolicyFindingStore
{
    /// <summary>Records a policy violation observed during an org's update.</summary>
    void Record(string org, PolicyViolationV2 finding);

    /// <summary>All findings for an org, newest first.</summary>
    IReadOnlyList<PolicyViolationV2> List(string org);
}
