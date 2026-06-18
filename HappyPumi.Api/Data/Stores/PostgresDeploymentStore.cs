#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IDeploymentStore"/> (ADR-0005).</summary>
public sealed class PostgresDeploymentStore(HappyPumiDbContext db) : IDeploymentStore
{
    public DeploymentSettings? GetSettings(StackCoordinates s)
        => SettingsRow(s)?.Settings;

    public void SetSettings(StackCoordinates s, DeploymentSettings settings)
    {
        var row = SettingsRow(s);
        if (row is null)
            db.DeploymentSettings.Add(new DeploymentSettingsRow { Org = s.Org, Project = s.Project, Stack = s.Stack, Settings = settings });
        else
            row.Settings = settings;
        db.SaveChanges();
    }

    public bool DeleteSettings(StackCoordinates s)
    {
        var row = SettingsRow(s);
        if (row is null)
            return false;
        db.DeploymentSettings.Remove(row);
        db.SaveChanges();
        return true;
    }

    public StoredDeployment CreateDeployment(StackCoordinates s, string operation)
    {
        var existing = db.Deployments.Where(d => d.Org == s.Org && d.Project == s.Project && d.Stack == s.Stack)
            .Select(d => d.Version).ToList();
        var version = existing.Count == 0 ? 1 : existing.Max() + 1;
        var row = new DeploymentRow
        {
            Id = Guid.NewGuid().ToString(), Org = s.Org, Project = s.Project, Stack = s.Stack,
            Version = version, Operation = operation,
        };
        db.Deployments.Add(row);
        db.SaveChanges();
        return new StoredDeployment { Id = row.Id, Version = row.Version, Operation = row.Operation };
    }

    public IReadOnlyList<StoredDeployment> ListDeployments(StackCoordinates s)
        => db.Deployments.AsNoTracking().Where(d => d.Org == s.Org && d.Project == s.Project && d.Stack == s.Stack)
            .OrderBy(d => d.Version).ToList()
            .Select(d => new StoredDeployment { Id = d.Id, Version = d.Version, Operation = d.Operation }).ToList();

    public bool CancelDeployment(StackCoordinates s, string deploymentId)
    {
        var row = db.Deployments.FirstOrDefault(d =>
            d.Org == s.Org && d.Project == s.Project && d.Stack == s.Stack && d.Id == deploymentId);
        if (row is null)
            return false;
        db.Deployments.Remove(row);
        db.SaveChanges();
        return true;
    }

    public void AddSchedule(StackCoordinates s, ScheduledAction schedule)
    {
        db.Schedules.Add(new ScheduleRow
        {
            Id = schedule.Id, Org = s.Org, Project = s.Project, Stack = s.Stack, Schedule = schedule,
        });
        db.SaveChanges();
    }

    public IReadOnlyList<ScheduledAction> ListSchedules(StackCoordinates s)
        => db.Schedules.AsNoTracking().Where(x => x.Org == s.Org && x.Project == s.Project && x.Stack == s.Stack)
            .ToList().Select(x => x.Schedule).ToList();

    public void AddWebhook(StackCoordinates s, WebhookResponse webhook)
    {
        db.Webhooks.Add(new WebhookRow
        {
            Id = Guid.NewGuid().ToString(), Org = s.Org, Project = s.Project, Stack = s.Stack, Webhook = webhook,
        });
        db.SaveChanges();
    }

    public IReadOnlyList<WebhookResponse> ListWebhooks(StackCoordinates s)
        => db.Webhooks.AsNoTracking().Where(x => x.Org == s.Org && x.Project == s.Project && x.Stack == s.Stack)
            .ToList().Select(x => x.Webhook).ToList();

    private DeploymentSettingsRow? SettingsRow(StackCoordinates s)
        => db.DeploymentSettings.FirstOrDefault(x => x.Org == s.Org && x.Project == s.Project && x.Stack == s.Stack);
}
