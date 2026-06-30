#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using Microsoft.Extensions.Logging;

namespace HappyPumi.Api.Webhooks;

/// <summary>
/// Best-effort firing of a stack's webhooks on a real event (update completion, deployment status change), plus
/// the owning organization's webhooks — org webhooks receive org-wide activity, so the same event is fired to
/// both scopes. Failures are swallowed (logged) per scope so a webhook problem can never fault or block the
/// triggering update/deployment.
/// </summary>
public sealed class StackWebhookTrigger(
    IDeploymentStore deployments, IOrgWebhookStore orgWebhooks,
    IWebhookDispatcher dispatcher, ILogger<StackWebhookTrigger> logger)
{
    public async Task FireAsync(StackCoordinates stack, string @event, object payload, CancellationToken ct)
    {
        await FireScopeAsync(new WebhookScope("stack", stack.Qualified), deployments.ListWebhooks(stack), @event, payload, ct);
        await FireScopeAsync(new WebhookScope("org", stack.Org), orgWebhooks.List(stack.Org), @event, payload, ct);
    }

    private async Task FireScopeAsync(
        WebhookScope scope, IReadOnlyList<WebhookResponse> webhooks, string @event, object payload, CancellationToken ct)
    {
        try
        {
            if (webhooks.Count == 0)
                return;
            await dispatcher.FireAsync(scope, webhooks, @event, payload, ct);
        }
        catch (Exception ex) // never let a webhook failure bubble into the update/deployment path
        {
            logger.LogWarning(ex, "{Scope} webhook firing failed for {Id} event {Event}", scope.Kind, scope.Id, @event);
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
