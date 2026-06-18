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

/// <summary>A deployment record. Key: Id.</summary>
public sealed class DeploymentRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Stack { get; set; } = default!;
    public long Version { get; set; }
    public string Operation { get; set; } = "update";
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
