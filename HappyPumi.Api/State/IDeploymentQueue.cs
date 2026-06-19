#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Data.Entities;

namespace HappyPumi.Api.State;

/// <summary>
/// The runner work queue behind Pulumi Deployments. The customer-managed workflow agent claims queued
/// deployments via GET /api/deployments/poll, then reports progress through the workflow-job callbacks.
/// Reverse-engineered from the prebuilt agent's wire behaviour (see workspace research/, ADR-0008 black-box).
/// </summary>
public interface IDeploymentQueue
{
    /// <summary>Atomically claims the oldest <c>not-started</c> deployment, stamping a job id/token and
    /// flipping it to <c>accepted</c>. Returns null when the queue is empty (poll → 204).</summary>
    DeploymentRow? ClaimNext();

    /// <summary>The claimed deployment a runner is asking about, by its job id. Null if unknown.</summary>
    DeploymentRow? GetByJobId(string jobId);

    /// <summary>A deployment by its deployment id (the agent's workflow status check). Null if unknown.</summary>
    DeploymentRow? GetByDeploymentId(string deploymentId);

    /// <summary>Moves a job to a new status (running/succeeded/failed). Returns false if the job is unknown.</summary>
    bool SetStatusByJobId(string jobId, string status);

    /// <summary>Seeds the deployment's job/step timeline from the job definition's step names (idempotent —
    /// called when the runner first fetches the job). Populates the console's deployment "Steps" panel.</summary>
    void RecordJobSteps(string jobId, IReadOnlyList<string> stepNames);

    /// <summary>Updates one step's status and timestamps as the runner reports per-step progress.</summary>
    void RecordStepStatus(string jobId, int stepIndex, string status);

    /// <summary>Appends a runner log line for a job step.</summary>
    void AppendLog(string jobId, int step, string line);

    /// <summary>All log lines recorded for a deployment, in order.</summary>
    IReadOnlyList<DeploymentLogRow> GetLogs(string deploymentId);
}
