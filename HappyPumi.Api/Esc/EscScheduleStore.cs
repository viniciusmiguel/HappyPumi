#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>
/// Persistence seam for an environment's scheduled actions (scheduled deletion / rotation, cron or one-shot).
/// Backed by PostgreSQL (see <c>PostgresEscScheduleStore</c>).
/// </summary>
public interface IEscScheduleStore
{
    ScheduledAction Add(EnvCoordinates environment, ScheduledAction action);
    IReadOnlyList<ScheduledAction> List(EnvCoordinates environment);
    ScheduledAction? Get(EnvCoordinates environment, string scheduleId);
    bool Remove(EnvCoordinates environment, string scheduleId);
    /// <summary>Applies an edit to a stored schedule (and bumps Modified). Null when it does not exist.</summary>
    ScheduledAction? Mutate(EnvCoordinates environment, string scheduleId, Action<ScheduledAction> edit);
}
