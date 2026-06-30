#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.State;
using Microsoft.Extensions.Logging;

namespace HappyPumi.Api.Webhooks;

/// <summary>
/// Best-effort firing of a stack's webhooks on a real event (update completion, deployment status change).
/// Resolves the stack's webhooks and hands them to the dispatcher. Failures are swallowed (logged) so a
/// webhook problem can never fault or block the triggering update/deployment.
/// </summary>
public sealed class StackWebhookTrigger(
    IDeploymentStore deployments, IWebhookDispatcher dispatcher, ILogger<StackWebhookTrigger> logger)
{
    public async Task FireAsync(StackCoordinates stack, string @event, object payload, CancellationToken ct)
    {
        try
        {
            var webhooks = deployments.ListWebhooks(stack);
            if (webhooks.Count == 0)
                return;
            await dispatcher.FireAsync(new WebhookScope("stack", stack.Qualified), webhooks, @event, payload, ct);
        }
        catch (Exception ex) // never let a webhook failure bubble into the update/deployment path
        {
            logger.LogWarning(ex, "Stack webhook firing failed for {Stack} event {Event}", stack.Qualified, @event);
        }
    }

    /// <summary>The summary payload sent for a completed stack update.</summary>
    public static object StackUpdateEvent(StoredUpdate update, string result) => new
    {
        organization = update.Coordinates.Org,
        project = update.Coordinates.Project,
        stackName = update.Coordinates.Stack,
        kind = update.Kind,
        result,
        updateId = update.UpdateId,
        version = update.Version,
    };
}
