#nullable enable

using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Data.Entities;

/// <summary>Managed-deployment settings for a stack. Key: (Org, Project, Stack). Settings are jsonb.</summary>
public sealed class DeploymentSettingsRow
{
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Stack { get; set; } = default!;
    public DeploymentSettings Settings { get; set; } = default!;
}

/// <summary>
/// A managed-deployment record and its place in the runner work queue. Key: Id. A deployment is created
/// in status <c>not-started</c>; the customer-managed workflow agent claims it via GET /api/deployments/poll
/// (which flips it to <c>accepted</c> and stamps a JobId/JobToken), then reports it running → succeeded/failed.
/// </summary>
public sealed class DeploymentRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Stack { get; set; } = default!;
    public long Version { get; set; }
    public string Operation { get; set; } = "update";

    /// <summary>not-started | accepted | running | succeeded | failed (apitype deployment status values).</summary>
    public string Status { get; set; } = "not-started";
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the agent claims the job; the runner authenticates callbacks with JobToken.</summary>
    public string? JobId { get; set; }
    public string? JobToken { get; set; }

    // Console-facing detail. The user who requested the deployment (scalar for listing/sorting).
    public string? RequestedByLogin { get; set; }
    public string? RequestedByName { get; set; }
    /// <summary>Template ref ("source/publisher/name/version") the runner deploys, when this is a template deploy.</summary>
    public string? TemplateRef { get; set; }
    /// <summary>Job runs with their step timeline (jsonb — nested contract payload).</summary>
    public List<DeploymentJob> Jobs { get; set; } = new();
    /// <summary>Stack updates produced by this deployment (jsonb).</summary>
    public List<DeploymentNestedUpdate> Updates { get; set; } = new();
}

/// <summary>One log line a runner appended for a deployment job step. Key: Id (sequence).</summary>
public sealed class DeploymentLogRow
{
    public long Id { get; set; }
    public string DeploymentId { get; set; } = default!;
    public int Step { get; set; }
    /// <summary>Log source label shown by the console (e.g. "pulumi").</summary>
    public string? Header { get; set; }
    public string Line { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>A scheduled action on a stack. Key: Id. The action definition is jsonb.</summary>
public sealed class ScheduleRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Stack { get; set; } = default!;
    public ScheduledAction Schedule { get; set; } = default!;
}

/// <summary>A stack webhook. Key: Id. The webhook payload is jsonb.</summary>
public sealed class WebhookRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Stack { get; set; } = default!;
    public WebhookResponse Webhook { get; set; } = default!;
}
