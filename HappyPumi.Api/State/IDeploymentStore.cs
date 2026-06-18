#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for managed deployments (ENDPOINTS.md 6): per-stack deployment settings, deployment
/// records, schedules, and webhooks. In-memory by default (ADR-0005). Safe for concurrent use.
/// </summary>
public interface IDeploymentStore
{
    DeploymentSettings? GetSettings(StackCoordinates stack);
    void SetSettings(StackCoordinates stack, DeploymentSettings settings);
    bool DeleteSettings(StackCoordinates stack);

    /// <summary>Creates a deployment record, returning it (with an id and auto-incremented version).</summary>
    StoredDeployment CreateDeployment(StackCoordinates stack, string operation);
    IReadOnlyList<StoredDeployment> ListDeployments(StackCoordinates stack);

    /// <summary>Cancels (removes) a deployment. Returns false when no such deployment exists.</summary>
    bool CancelDeployment(StackCoordinates stack, string deploymentId);

    void AddSchedule(StackCoordinates stack, ScheduledAction schedule);
    IReadOnlyList<ScheduledAction> ListSchedules(StackCoordinates stack);

    void AddWebhook(StackCoordinates stack, WebhookResponse webhook);
    IReadOnlyList<WebhookResponse> ListWebhooks(StackCoordinates stack);
}
