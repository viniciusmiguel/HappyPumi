#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>
/// Holds an environment's scheduled actions (scheduled deletion / rotation, cron or one-shot). In-memory for
/// now — the schedules are recorded and managed; a background executor that fires them is a follow-up.
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

/// <summary>In-memory <see cref="IEscScheduleStore"/>, keyed by (environment, schedule id).</summary>
public sealed class EscScheduleStore : IEscScheduleStore
{
    private readonly ConcurrentDictionary<string, ScheduledAction> _schedules = new();

    private static string Key(EnvCoordinates e, string id) => $"{e.Org}/{e.Project}/{e.Name}/{id}";
    private static string Prefix(EnvCoordinates e) => $"{e.Org}/{e.Project}/{e.Name}/";

    public ScheduledAction Add(EnvCoordinates environment, ScheduledAction action)
    {
        _schedules[Key(environment, action.Id)] = action;
        return action;
    }

    public IReadOnlyList<ScheduledAction> List(EnvCoordinates environment)
        => _schedules.Where(kv => kv.Key.StartsWith(Prefix(environment))).Select(kv => kv.Value)
            .OrderBy(a => a.Created).ToList();

    public ScheduledAction? Get(EnvCoordinates environment, string scheduleId)
        => _schedules.GetValueOrDefault(Key(environment, scheduleId));

    public bool Remove(EnvCoordinates environment, string scheduleId)
        => _schedules.TryRemove(Key(environment, scheduleId), out _);

    public ScheduledAction? Mutate(EnvCoordinates environment, string scheduleId, Action<ScheduledAction> edit)
    {
        if (!_schedules.TryGetValue(Key(environment, scheduleId), out var action))
            return null;
        edit(action);
        action.Modified = DateTime.UtcNow.ToString("o");
        return action;
    }
}
