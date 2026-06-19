#nullable enable

using System;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// Durable rows for the ESC operational state that was previously in-memory (schedules, drafts, open
/// requests, rotation history, webhook deliveries). Each carries the env coordinates for scoping plus the
/// domain/contract payload as jsonb. Opened-environment sessions are deliberately NOT here — they hold
/// freshly decrypted secrets and short-lived credentials and stay in-memory (ADR-0005).
/// </summary>
public sealed class EnvironmentScheduleRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public ScheduledAction Action { get; set; } = default!;
}

public sealed class EnvironmentDraftRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Name { get; set; } = default!;
    public EscDraft Draft { get; set; } = default!;
}

public sealed class EnvironmentOpenRequestRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Name { get; set; } = default!;
    public EscOpenRequest Request { get; set; } = default!;
}

public sealed class EnvironmentRotationEventRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public SecretRotationEvent Event { get; set; } = default!;
}

public sealed class EnvironmentWebhookDeliveryRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Project { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string HookName { get; set; } = default!;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public WebhookDelivery Delivery { get; set; } = default!;
}
