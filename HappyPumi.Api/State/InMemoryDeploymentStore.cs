#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IDeploymentStore"/> (ADR-0005), keyed by stack coordinates.</summary>
public sealed class InMemoryDeploymentStore : IDeploymentStore
{
    private readonly ConcurrentDictionary<StackCoordinates, StackDeploymentState> _state = new();

    private StackDeploymentState State(StackCoordinates stack)
        => _state.GetOrAdd(stack, _ => new StackDeploymentState());

    public DeploymentSettings? GetSettings(StackCoordinates stack) => State(stack).Settings;

    public void SetSettings(StackCoordinates stack, DeploymentSettings settings) => State(stack).Settings = settings;

    public bool DeleteSettings(StackCoordinates stack)
    {
        var state = State(stack);
        if (state.Settings is null)
            return false;
        state.Settings = null;
        return true;
    }

    public StoredDeployment CreateDeployment(StackCoordinates stack, string operation)
    {
        var state = State(stack);
        lock (state.Deployments)
        {
            var deployment = new StoredDeployment
            {
                Id = Guid.NewGuid().ToString(),
                Version = state.NextVersion++,
                Operation = operation,
            };
            state.Deployments.Add(deployment);
            return deployment;
        }
    }

    public IReadOnlyList<StoredDeployment> ListDeployments(StackCoordinates stack)
    {
        var state = State(stack);
        lock (state.Deployments)
            return state.Deployments.ToArray();
    }

    public IReadOnlyList<StoredDeployment> ListByOrg(string org)
        => _state.Where(kv => kv.Key.Org == org)
            .SelectMany(kv => kv.Value.Deployments)
            .OrderByDescending(d => d.Created).ToArray();

    public StoredDeployment? GetByVersion(StackCoordinates stack, long version)
    {
        var state = State(stack);
        lock (state.Deployments)
            return state.Deployments.FirstOrDefault(d => d.Version == version);
    }

    public StoredDeployment? GetById(StackCoordinates stack, string deploymentId)
    {
        var state = State(stack);
        lock (state.Deployments)
            return state.Deployments.FirstOrDefault(d => d.Id == deploymentId);
    }

    // The in-memory store does not retain log lines (logs are persisted by the Postgres store/runner).
    public IReadOnlyList<DeploymentLogLine> GetLogs(string deploymentId) => Array.Empty<DeploymentLogLine>();

    public bool CancelDeployment(StackCoordinates stack, string deploymentId)
    {
        var state = State(stack);
        lock (state.Deployments)
            return state.Deployments.RemoveAll(d => d.Id == deploymentId) > 0;
    }

    public void AddSchedule(StackCoordinates stack, ScheduledAction schedule)
    {
        var state = State(stack);
        lock (state.Schedules)
            state.Schedules.Add(schedule);
    }

    public IReadOnlyList<ScheduledAction> ListSchedules(StackCoordinates stack)
    {
        var state = State(stack);
        lock (state.Schedules)
            return state.Schedules.ToArray();
    }

    public void AddWebhook(StackCoordinates stack, WebhookResponse webhook)
    {
        var state = State(stack);
        lock (state.Webhooks)
            state.Webhooks.Add(webhook);
    }

    public IReadOnlyList<WebhookResponse> ListWebhooks(StackCoordinates stack)
    {
        var state = State(stack);
        lock (state.Webhooks)
            return state.Webhooks.ToArray();
    }
}
