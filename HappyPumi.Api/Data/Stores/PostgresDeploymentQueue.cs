#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IDeploymentQueue"/> (ADR-0005).</summary>
public sealed class PostgresDeploymentQueue(HappyPumiDbContext db) : IDeploymentQueue
{
    public DeploymentRow? ClaimNext()
    {
        // Oldest queued deployment first. A row-lock would be needed for multiple agents; one agent here.
        var row = db.Deployments
            .Where(d => d.Status == "not-started")
            .OrderBy(d => d.Created)
            .FirstOrDefault();
        if (row is null)
            return null;

        row.Status = "accepted";
        row.JobId = "job-" + Guid.NewGuid().ToString("N");
        row.JobToken = "jt-" + Guid.NewGuid().ToString("N");
        row.Modified = DateTime.UtcNow;
        db.SaveChanges();
        return row;
    }

    public DeploymentRow? GetByJobId(string jobId)
        => db.Deployments.FirstOrDefault(d => d.JobId == jobId);

    public DeploymentRow? GetByDeploymentId(string deploymentId)
        => db.Deployments.FirstOrDefault(d => d.Id == deploymentId);

    public bool SetStatusByJobId(string jobId, string status)
    {
        var row = db.Deployments.FirstOrDefault(d => d.JobId == jobId);
        if (row is null)
            return false;
        // The runner reports per-step status; we surface the worst as the deployment status. "failed" is
        // sticky so a later step's "succeeded" (e.g. the runner-skipped placeholder step 0) cannot mask a
        // real failure regardless of callback ordering.
        if (row.Status == "failed" && status != "failed")
            return true;
        row.Status = status;
        row.Modified = DateTime.UtcNow;
        db.SaveChanges();
        return true;
    }

    public void RecordJobSteps(string jobId, IReadOnlyList<string> stepNames)
    {
        var row = db.Deployments.FirstOrDefault(d => d.JobId == jobId);
        if (row is null || row.Jobs.Count > 0)
            return; // unknown job, or already seeded (the runner may re-fetch the definition)
        var now = DateTime.UtcNow;
        row.Jobs = new List<DeploymentJob>
        {
            new()
            {
                Status = "running", Started = now, LastUpdated = now,
                Steps = stepNames.Select(name => new StepRun { Name = name, Status = "not-started" }).ToList(),
            },
        };
        row.Modified = now;
        db.SaveChanges();
    }

    public void RecordStepStatus(string jobId, int stepIndex, string status)
    {
        var row = db.Deployments.FirstOrDefault(d => d.JobId == jobId);
        if (row is null || row.Jobs.Count == 0)
            return;
        var job = row.Jobs[0];
        if (stepIndex < 0 || stepIndex >= job.Steps.Count)
            return;
        var now = DateTime.UtcNow;
        var step = job.Steps[stepIndex];
        step.Started ??= now;
        step.Status = status;
        step.LastUpdated = now;
        job.LastUpdated = now;
        // The job fails if any step fails (sticky), and succeeds once every step has succeeded.
        if (status == "failed")
            job.Status = "failed";
        else if (job.Status != "failed" && job.Steps.All(s => s.Status == "succeeded"))
            job.Status = "succeeded";
        // EF's jsonb value-comparer snapshots a deep clone at load, so the in-place mutations above are
        // detected at SaveChanges (no reassignment needed).
        row.Modified = now;
        db.SaveChanges();
    }

    public void AppendLog(string jobId, int step, string line)
    {
        var deploymentId = db.Deployments.Where(d => d.JobId == jobId).Select(d => d.Id).FirstOrDefault();
        if (deploymentId is null)
            return;
        db.DeploymentLogs.Add(new DeploymentLogRow
        {
            DeploymentId = deploymentId, Step = step, Line = line, Timestamp = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    public IReadOnlyList<DeploymentLogRow> GetLogs(string deploymentId)
        => db.DeploymentLogs.AsNoTracking()
            .Where(l => l.DeploymentId == deploymentId)
            .OrderBy(l => l.Id)
            .ToList();
}
