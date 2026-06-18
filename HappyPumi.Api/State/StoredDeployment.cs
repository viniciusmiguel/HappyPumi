#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>A managed-deployment record created for a stack (ENDPOINTS.md 6).</summary>
public sealed class StoredDeployment
{
    public required string Id { get; init; }
    public required long Version { get; init; }
    public string Operation { get; set; } = "update";

    // Console-facing detail (populated by reads; the runner create path leaves these at defaults).
    public string Org { get; set; } = "";
    public string Project { get; set; } = "";
    public string Stack { get; set; } = "";
    public string Status { get; set; } = "not-started";
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public string? RequestedByLogin { get; set; }
    public string? RequestedByName { get; set; }
    public List<DeploymentJob> Jobs { get; set; } = new();
    public List<DeploymentNestedUpdate> Updates { get; set; } = new();
}

/// <summary>All managed-deployment state attached to one stack: settings, deployments, schedules, webhooks.</summary>
public sealed class StackDeploymentState
{
    public DeploymentSettings? Settings { get; set; }
    public List<StoredDeployment> Deployments { get; } = new();
    public List<ScheduledAction> Schedules { get; } = new();
    public List<WebhookResponse> Webhooks { get; } = new();
    public long NextVersion { get; set; } = 1;
}
