#nullable enable

using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Shared reapproval policy for draft updates (change-requests PR3, <c>requireReapprovalOnChange</c>). Editing
/// a draft's YAML after it has accrued approvals invalidates them when any applicable gate demands reapproval,
/// so the change request must collect fresh sign-offs before apply. Used by both the ESC draft-update endpoint
/// and its <c>/api/preview/</c> alias to keep the rule in one place.
/// </summary>
internal static class ChangeRequestReapproval
{
    /// <summary>Clears the CR's approvers when it has any and an applicable gate requires reapproval on change.</summary>
    public static void ClearIfRequired(
        IChangeRequestStore changeRequests, ChangeGateEvaluator evaluator, string org, string id)
    {
        var cr = changeRequests.Get(org, id);
        if (cr is null || !cr.Approvers.Any() || !evaluator.RequiresReapproval(cr))
            return;
        changeRequests.Update(org, id, c => c.Approvers.Clear());
    }
}
