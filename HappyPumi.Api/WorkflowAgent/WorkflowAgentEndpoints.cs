#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.WorkflowAgent;

// Endpoints the prebuilt Pulumi customer-managed workflow agent (v2.2.0) calls. These paths are NOT in the
// public OpenAPI spec; they were reverse-engineered black-box by observing the agent on the wire (see the
// workspace research/ notes, ADR-0008). All are anonymous and read the agent-pool token loosely — the agent
// sends `Authorization: token <pool-token>`.

/// <summary>GET /api/background-activities/token — issue a worker token for the agent's background poller.</summary>
public sealed class BackgroundActivitiesTokenEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/background-activities/token");
        AllowAnonymous();
        Description(b => b.WithTags("WorkflowAgent").WithName("BackgroundActivitiesToken"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var expiration = DateTime.UtcNow.AddHours(1).ToString("o");
        await Send.OkAsync(new { token = "hp-worker-" + Guid.NewGuid().ToString("N"), expiration }, ct);
    }
}

/// <summary>
/// POST /api/background-activities/configuration — the agent unmarshals this into a JSON ARRAY of
/// BackgroundActivityConfiguration and starts its spooler. (Object → unmarshal error; confirmed live.)
/// </summary>
public sealed class BackgroundActivitiesConfigurationEndpoint : EndpointWithoutRequest<List<BackgroundActivityConfiguration>>
{
    public override void Configure()
    {
        Post("/api/background-activities/configuration");
        AllowAnonymous();
        Description(b => b.WithTags("WorkflowAgent").WithName("BackgroundActivitiesConfiguration"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var pool = new AgentPoolRef { Id = "pool-1", Name = "default", Description = "HappyPumi default pool" };
        await Send.OkAsync(new List<BackgroundActivityConfiguration>
        {
            new() { Kind = "deployment", AgentPools = { pool } },
        }, ct);
    }
}

/// <summary>
/// POST /api/background-activities/worker/lease/acquire — leasing for non-deployment background activities
/// (Insights discovery, Policy evaluation). HappyPumi has no such work, so always 204 (no content).
/// Deployments do NOT come through here; they use GET /api/deployments/poll.
/// </summary>
public sealed class AcquireWorkerLeaseEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/background-activities/worker/lease/acquire");
        AllowAnonymous();
        Description(b => b.WithTags("WorkflowAgent").WithName("AcquireWorkerLease"));
    }

    public override async Task HandleAsync(CancellationToken ct)
        => await Send.NoContentAsync(ct);
}

/// <summary>POST /api/agent-workflows/deployment — the agent acks a claimed deployment workflow.</summary>
public sealed class AckDeploymentWorkflowEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/agent-workflows/deployment");
        AllowAnonymous();
        Description(b => b.WithTags("WorkflowAgent").WithName("AckDeploymentWorkflow"));
    }

    public override async Task HandleAsync(CancellationToken ct)
        => await Send.OkAsync(new { }, ct);
}

/// <summary>
/// POST /api/agent-workflows/{agentJobID}/check — the agent checks/fetches a workflow's status.
/// agentJobID is "<type>:<id>" (e.g. "deployment:&lt;deploymentID&gt;"). Returns the deployment's status.
/// </summary>
public sealed class AgentWorkflowCheckEndpoint(IDeploymentQueue queue) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/agent-workflows/{agentJobID}/check");
        AllowAnonymous();
        Description(b => b.WithTags("WorkflowAgent").WithName("AgentWorkflowCheck"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var agentJobId = Route<string>("agentJobID") ?? "";
        var deploymentId = agentJobId.Contains(':') ? agentJobId[(agentJobId.IndexOf(':') + 1)..] : agentJobId;
        var row = queue.GetByDeploymentId(deploymentId);
        var status = row?.Status ?? "not-started";
        await Send.OkAsync(new { id = deploymentId, status, complete = status is "succeeded" or "failed" }, ct);
    }
}

public sealed class JobIdRequest
{
    public string JobID { get; set; } = default!;
}

/// <summary>GET /api/workflow/jobs/{jobID} — the job definition the workflow-runner executes.</summary>
public sealed class GetWorkflowJobEndpoint(IDeploymentQueue queue) : Endpoint<JobIdRequest, JobDefinition>
{
    public override void Configure()
    {
        Get("/api/workflow/jobs/{jobID}");
        AllowAnonymous();
        Description(b => b.WithTags("WorkflowAgent").WithName("GetWorkflowJob"));
    }

    public override async Task HandleAsync(JobIdRequest req, CancellationToken ct)
    {
        var row = queue.GetByJobId(req.JobID);
        if (row is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new JobDefinition
        {
            Os = "linux",
            Architecture = "amd64",
            Image = new JobImage { Reference = "pulumi/pulumi-base:latest" },
            Shell = "bash",
            Steps =
            {
                // Smoke step proving the executor runs pulumi and reports back. A real deployment would
                // clone the stack's source and run `pulumi <op> --yes` here (deployment settings phase).
                new StepDefinition { Name = "pulumi " + row.Operation, Run = "pulumi version" },
            },
        }, ct);
    }
}

/// <summary>
/// PATCH /api/workflow/jobs/{jobID}/{stepIndex}/status — runner reports a step's status. The runner sends
/// the body WITHOUT a Content-Type, so we bind route + body manually (FE body binding would 415).
/// </summary>
public sealed class UpdateStepStatusEndpoint(IDeploymentQueue queue) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Patch("/api/workflow/jobs/{jobID}/{stepIndex}/status");
        AllowAnonymous();
        Description(b => b.WithTags("WorkflowAgent").WithName("UpdateStepStatus"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var jobId = Route<string>("jobID") ?? "";
        var status = await ReadJsonStringFieldAsync(HttpContext, "status", ct);
        if (!string.IsNullOrWhiteSpace(status))
            queue.SetStatusByJobId(jobId, status switch
            {
                "succeeded" => "succeeded",
                "failed" => "failed",
                _ => "running",
            });
        await Send.OkAsync(new { }, ct);
    }

    /// <summary>Reads one top-level string field from a JSON body that may arrive without a Content-Type.</summary>
    internal static async Task<string?> ReadJsonStringFieldAsync(HttpContext ctx, string field, CancellationToken ct)
    {
        ctx.Request.EnableBuffering();
        using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(ct);
        ctx.Request.Body.Position = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.TryGetProperty(field, out var v) ? v.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}

/// <summary>POST /api/workflow/jobs/{jobID}/{stepIndex}/logs — runner appends step log lines (no Content-Type).</summary>
public sealed class AppendStepLogsEndpoint(IDeploymentQueue queue) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/workflow/jobs/{jobID}/{stepIndex}/logs");
        AllowAnonymous();
        Description(b => b.WithTags("WorkflowAgent").WithName("AppendStepLogs"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var jobId = Route<string>("jobID") ?? "";
        var step = Route<int?>("stepIndex") ?? 0;
        using var reader = new StreamReader(HttpContext.Request.Body);
        var raw = await reader.ReadToEndAsync(ct);
        if (!string.IsNullOrWhiteSpace(raw))
            queue.AppendLog(jobId, step, raw.Trim());
        await Send.OkAsync(new { }, ct);
    }
}

/// <summary>POST /api/workflow/jobs/{jobID}/heartbeat — keep the job alive while the runner works.</summary>
public sealed class WorkflowJobHeartbeatEndpoint : Endpoint<JobIdRequest>
{
    public override void Configure()
    {
        Post("/api/workflow/jobs/{jobID}/heartbeat");
        AllowAnonymous();
        Description(b => b.WithTags("WorkflowAgent").WithName("WorkflowJobHeartbeat"));
    }

    public override async Task HandleAsync(JobIdRequest req, CancellationToken ct)
        => await Send.OkAsync(new { }, ct);
}
