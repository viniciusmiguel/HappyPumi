#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Drives an update through its lifecycle and reconciles the result with stack state. Kind-agnostic, so
/// the update/preview/refresh/destroy endpoints (ENDPOINTS.md 1c/1d) all delegate here, passing their
/// route's <c>kind</c>. The single rule that differs by kind is that <c>preview</c> is a dry run whose
/// checkpoints are never promoted to the stack.
/// </summary>
public sealed class UpdateLifecycle(IUpdateStore updates, IStackStore stacks)
{
    public const string PreviewKind = "preview";

    /// <summary>
    /// Creates an update for an existing stack, capturing the program's config/message so the history
    /// and /updates/latest can replay them. Returns null when the stack does not exist.
    /// </summary>
    public StoredUpdate? Create(
        StackCoordinates stack, string kind, AppUpdateProgramRequest? program = null, UpdateActor? requestedBy = null)
    {
        if (stacks.Find(stack) is null)
            return null;
        var update = updates.Create(stack, kind, dryRun: kind == PreviewKind);
        update.Config = program?.Config;
        update.Message = program?.Metadata?.Message ?? string.Empty;
        update.RequestedByLogin = requestedBy?.Login;
        update.RequestedByName = requestedBy?.Name;
        updates.Save(update);
        return update;
    }

    /// <summary>
    /// Starts the update: marks it running, issues a lease token, and computes the version the stack will
    /// have once it completes (current + 1). Returns null when the update or its stack is gone.
    /// </summary>
    public StoredUpdate? Start(string updateId)
    {
        var update = updates.Find(updateId);
        var stack = update is null ? null : stacks.Find(update.Coordinates);
        if (update is null || stack is null)
            return null;
        update.Status = UpdateStatuses.Running;
        update.Token = $"lease-{Guid.NewGuid():N}";
        update.Version = stack.Version + 1;
        update.StartedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        updates.Save(update);
        return update;
    }

    /// <summary>Records the latest checkpoint for the update. Returns null when the update is unknown.</summary>
    public StoredUpdate? SaveCheckpoint(string updateId, AppUntypedDeployment deployment)
    {
        var update = updates.Find(updateId);
        if (update is null)
            return null;
        update.Checkpoint = deployment;
        updates.Save(update);
        return update;
    }

    /// <summary>
    /// Completes the update with the given status. On success (and unless it is a dry run) the last
    /// checkpoint is promoted to the stack and the stack version is bumped. Returns null when unknown.
    /// </summary>
    public StoredUpdate? Complete(string updateId, string status)
    {
        var update = updates.Find(updateId);
        if (update is null)
            return null;
        update.Status = status;
        updates.Save(update);
        if (status == UpdateStatuses.Succeeded && !update.DryRun && update.Checkpoint is not null)
            stacks.SetDeployment(update.Coordinates, update.Checkpoint, bumpVersion: true);

        // Record real updates (not dry-run previews) in the stack's history so `pulumi stack history`
        // and /updates/latest can see them.
        if (!update.DryRun)
            stacks.RecordHistory(update.Coordinates, HistoryEntryFor(update, status));
        return update;
    }

    private static StoredHistoryEntry HistoryEntryFor(StoredUpdate update, string status) => new()
    {
        UpdateId = update.UpdateId,
        RequestedByLogin = update.RequestedByLogin,
        RequestedByName = update.RequestedByName,
        Info = new AppUpdateInfo
        {
            Kind = update.Kind,
            // CompleteUpdate's status ("succeeded"/"failed") is already the apitype UpdateResult value.
            Result = status,
            Message = update.Message,
            StartTime = update.StartedAt,
            EndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Version = update.Version,
            Config = update.Config ?? new Dictionary<string, AppConfigValue>(),
            Environment = new Dictionary<string, string>(),
        },
    };

    /// <summary>Marks the update failed (the CLI cancels, then completes). Returns null when unknown.</summary>
    public StoredUpdate? Cancel(string updateId)
    {
        var update = updates.Find(updateId);
        if (update is null)
            return null;
        update.Status = UpdateStatuses.Failed;
        updates.Save(update);
        return update;
    }

    public StoredUpdate? Find(string updateId) => updates.Find(updateId);
}
