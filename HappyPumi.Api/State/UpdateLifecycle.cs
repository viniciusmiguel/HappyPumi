#nullable enable

using System;
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

    /// <summary>Creates an update for an existing stack, or null when the stack does not exist.</summary>
    public StoredUpdate? Create(StackCoordinates stack, string kind)
    {
        if (stacks.Find(stack) is null)
            return null;
        return updates.Create(stack, kind, dryRun: kind == PreviewKind);
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
        return update;
    }

    /// <summary>Records the latest checkpoint for the update. Returns null when the update is unknown.</summary>
    public StoredUpdate? SaveCheckpoint(string updateId, AppUntypedDeployment deployment)
    {
        var update = updates.Find(updateId);
        if (update is null)
            return null;
        update.Checkpoint = deployment;
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
        if (status == UpdateStatuses.Succeeded && !update.DryRun && update.Checkpoint is not null)
            stacks.SetDeployment(update.Coordinates, update.Checkpoint, bumpVersion: true);
        return update;
    }

    /// <summary>Marks the update failed (the CLI cancels, then completes). Returns null when unknown.</summary>
    public StoredUpdate? Cancel(string updateId)
    {
        var update = updates.Find(updateId);
        if (update is null)
            return null;
        update.Status = UpdateStatuses.Failed;
        return update;
    }

    public StoredUpdate? Find(string updateId) => updates.Find(updateId);
}
