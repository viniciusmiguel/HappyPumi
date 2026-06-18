#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
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
        row.Status = status;
        row.Modified = DateTime.UtcNow;
        db.SaveChanges();
        return true;
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
