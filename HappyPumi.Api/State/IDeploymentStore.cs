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

    /// <summary>Creates a deployment record, returning it (with an id and auto-incremented version). When
    /// <paramref name="git"/> is set, the runner clones that repo and runs the operation there
    /// (remote-workspace path); when <paramref name="templateRef"/> is set, it deploys that registry
    /// template ("source/publisher/name/version"). The two source modes are mutually exclusive.</summary>
    StoredDeployment CreateDeployment(StackCoordinates stack, string operation, GitSource? git = null, string? templateRef = null);
    IReadOnlyList<StoredDeployment> ListDeployments(StackCoordinates stack);
    /// <summary>All deployments across an org, newest first (the org Deployments console page).</summary>
    IReadOnlyList<StoredDeployment> ListByOrg(string org);
    /// <summary>A single deployment by version within a stack, or null.</summary>
    StoredDeployment? GetByVersion(StackCoordinates stack, long version);
    /// <summary>A single deployment by id within a stack, or null.</summary>
    StoredDeployment? GetById(StackCoordinates stack, string deploymentId);
    /// <summary>Ordered log lines for a deployment (the console log panel).</summary>
    IReadOnlyList<DeploymentLogLine> GetLogs(string deploymentId);

    /// <summary>Cancels (removes) a deployment. Returns false when no such deployment exists.</summary>
    bool CancelDeployment(StackCoordinates stack, string deploymentId);

    void AddSchedule(StackCoordinates stack, ScheduledAction schedule);
    IReadOnlyList<ScheduledAction> ListSchedules(StackCoordinates stack);

    void AddWebhook(StackCoordinates stack, WebhookResponse webhook);
    IReadOnlyList<WebhookResponse> ListWebhooks(StackCoordinates stack);
    /// <summary>A single stack webhook by name, or null. Carries the secret for signing — sanitize before echoing.</summary>
    WebhookResponse? GetWebhook(StackCoordinates stack, string name);
    /// <summary>Applies a PATCH body to a stack webhook and returns the updated record; null when it does not exist.</summary>
    WebhookResponse? UpdateWebhook(StackCoordinates stack, string name, Webhook patch);
    /// <summary>Removes a stack webhook by name. False when no such webhook exists.</summary>
    bool DeleteWebhook(StackCoordinates stack, string name);
}
