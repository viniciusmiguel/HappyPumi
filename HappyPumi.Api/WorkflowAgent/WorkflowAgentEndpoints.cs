#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.Extensions.Configuration;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
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
public sealed class GetWorkflowJobEndpoint(IDeploymentQueue queue, IConfiguration config)
    : Endpoint<JobIdRequest, JobDefinition>
{
    // The backend URL the executor container uses to reach HappyPumi (registry archive + pulumi backend).
    // Must be reachable from inside the runner's nested executor container — the docker-bridge gateway by
    // default (same address the agent's service_url uses), overridable for other topologies.
    private string ExecutorBackendUrl =>
        config["WorkflowAgent:ExecutorBackendUrl"] ?? "http://172.17.0.1:5118";

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

        var job = BuildJob(row, ExecutorBackendUrl);
        // Seed the deployment's step timeline so the console's "Steps" panel reflects the runner's progress.
        queue.RecordJobSteps(req.JobID, job.Steps.Select(s => s.Name).ToList());
        await Send.OkAsync(job, ct);
    }

    /// <summary>
    /// Builds the runner job. A template deploy (<see cref="DeploymentRow.TemplateRef"/> set) emits steps that
    /// fetch the published template archive and run <c>pulumi up</c> against HappyPumi as the backend; otherwise
    /// a diagnostic <c>pulumi version</c> step (e.g. an empty-stack operation with no source to materialize).
    /// </summary>
    internal static JobDefinition BuildJob(DeploymentRow row, string backendUrl)
    {
        var job = new JobDefinition
        {
            Image = new JobImage { Reference = "pulumi/pulumi:latest" },
            Env =
            {
                ["PULUMI_BACKEND_URL"] = backendUrl,
                // Dev: HappyPumi accepts any non-empty access token (PulumiTokenAuthHandler). Reuse the job
                // token the agent already holds so logs trace back to this job.
                ["PULUMI_ACCESS_TOKEN"] = row.JobToken ?? "hp-runner",
                ["PULUMI_CONFIG_PASSPHRASE"] = "hp-demo",
            },
        };

        // The runner SKIPS step index 0 (its reserved source-checkout slot) and runs steps 1..N in a single
        // shared executor container (cwd "/", files persist across them). Verified live by probing the agent
        // (see research/ notes). So our real work must start at step 1, behind a placeholder step 0.
        job.Steps.Add(new StepDefinition { Name = "prepare", Run = "true" });

        if (string.IsNullOrWhiteSpace(row.TemplateRef))
        {
            job.Steps.Add(new StepDefinition { Name = "pulumi " + row.Operation, Run = "pulumi version" });
            return job;
        }

        job.Steps.Add(new StepDefinition { Name = "pulumi " + row.Operation, Run = DeployScript(row, backendUrl) });
        return job;
    }

    /// <summary>Fetches the published template archive, extracts it, then runs the requested Pulumi operation
    /// against the target stack — all in one step (see <see cref="BuildJob"/> for why it must be one step).</summary>
    private static string DeployScript(DeploymentRow row, string backendUrl)
    {
        var (source, publisher, name, version) = SplitTemplateRef(row.TemplateRef!);
        var url = $"{backendUrl}/api/registry/templates/{source}/{publisher}/{name}/versions/{version}/archive";
        var stackRef = $"{row.Org}/{row.Project}/{row.Stack}";
        var op = row.Operation is "preview" or "refresh" or "destroy" ? row.Operation : "up";
        var apply = op == "preview" ? "pulumi preview" : $"pulumi {op} --yes --skip-preview";
        // Use a fixed working dir (one container per step, so it is private) and avoid shell command
        // substitution — the runner mangles "$( … )" when it builds the container command, which silently
        // produced an empty no-op step (observed live).
        return string.Join('\n',
            "set -euo pipefail",
            "rm -rf /tmp/hpwork && mkdir -p /tmp/hpwork && cd /tmp/hpwork",
            $"curl -fsSL '{url}' -o template.tar.gz",
            "tar -xzf template.tar.gz",
            "ls -la",
            $"pulumi stack select --create '{stackRef}'",
            apply);
    }

    /// <summary>Splits a registry template ref "source/publisher/name/version" into its four parts.</summary>
    private static (string Source, string Publisher, string Name, string Version) SplitTemplateRef(string templateRef)
    {
        var p = templateRef.Split('/');
        if (p.Length != 4)
            throw new ArgumentException(
                $"template ref must be 'source/publisher/name/version', got '{templateRef}'", nameof(templateRef));
        return (p[0], p[1], p[2], p[3]);
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
        var stepIndex = Route<int?>("stepIndex") ?? 0;
        var status = await ReadJsonStringFieldAsync(HttpContext, "status", ct);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = NormalizeStatus(status);
            queue.SetStatusByJobId(jobId, normalized);          // overall deployment status (failed sticky)
            queue.RecordStepStatus(jobId, stepIndex, normalized); // per-step timeline for the console
        }
        await Send.OkAsync(new { }, ct);
    }

    /// <summary>Maps a runner step-status string (succeeded/failed/failure/success/… ) to a deployment status.</summary>
    internal static string NormalizeStatus(string status) => status.Trim().ToLowerInvariant() switch
    {
        "succeeded" or "success" or "complete" or "completed" => "succeeded",
        "failed" or "failure" or "error" or "errored" => "failed",
        _ => "running",
    };

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
        // Body is apitype.AppendStepLogsRequest { offset, lines:[{ t, l }] }; store each line ("l").
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
                    foreach (var l in lines.EnumerateArray())
                        if (l.TryGetProperty("l", out var line))
                            queue.AppendLog(jobId, step, line.GetString() ?? "");
            }
            catch (JsonException) { queue.AppendLog(jobId, step, raw.Trim()); }
        }
        await Send.OkAsync(new { }, ct);
    }
}

/// <summary>
/// POST /api/workflow/jobs/{jobID}/heartbeat — keep the job alive while the runner works. The runner sends
/// the heartbeat body WITHOUT a Content-Type, so this is bodyless (an <c>Endpoint&lt;TReq&gt;</c> would 415).
/// </summary>
public sealed class WorkflowJobHeartbeatEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/workflow/jobs/{jobID}/heartbeat");
        AllowAnonymous();
        Description(b => b.WithTags("WorkflowAgent").WithName("WorkflowJobHeartbeat"));
    }

    public override async Task HandleAsync(CancellationToken ct)
        => await Send.OkAsync(new { }, ct);
}
