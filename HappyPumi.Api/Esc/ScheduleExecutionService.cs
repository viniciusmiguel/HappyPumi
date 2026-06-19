#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using NCrontab;

namespace HappyPumi.Api.Esc;

/// <summary>
/// Runs one pass of due environment scheduled actions: for each non-paused schedule whose next occurrence is
/// at or before <c>now</c>, it fires the action (rotation runs the rotators; deletion soft-deletes the
/// environment) and advances the schedule. Separated from the background loop so it can be tested directly.
/// </summary>
public sealed class ScheduleExecutionService(IEscScheduleStore schedules, IEnvironmentStore environments, EscRotationRunner rotations)
{
    /// <summary>Fires every due schedule across all environments; returns how many fired.</summary>
    public async Task<int> RunDueAsync(DateTime nowUtc, CancellationToken ct)
    {
        var fired = 0;
        foreach (var env in environments.ListAll())
        {
            foreach (var schedule in schedules.List(env.Coordinates))
            {
                if (!IsDue(schedule, nowUtc))
                    continue;
                await FireAsync(env.Coordinates, schedule, ct);
                Advance(env.Coordinates, schedule, nowUtc);
                fired++;
            }
        }
        return fired;
    }

    private async Task FireAsync(EnvCoordinates coords, ScheduledAction schedule, CancellationToken ct)
    {
        if (schedule.Kind.Contains("delet", StringComparison.OrdinalIgnoreCase))
            environments.Delete(coords);
        else
            await rotations.RotateAsync(coords, "scheduler", ct);
    }

    // Cron fires when the next occurrence after the last run (or creation) is due; one-shot fires once.
    private static bool IsDue(ScheduledAction s, DateTime now)
    {
        if (s.Paused)
            return false;
        if (!string.IsNullOrWhiteSpace(s.ScheduleOnce))
            return string.IsNullOrEmpty(s.LastExecuted) && TryUtc(s.ScheduleOnce, out var once) && once <= now;
        if (!string.IsNullOrWhiteSpace(s.ScheduleCron) && TryCron(s.ScheduleCron!, out var cron))
        {
            var anchor = TryUtc(s.LastExecuted, out var last) ? last : (TryUtc(s.Created, out var c) ? c : now);
            return cron.GetNextOccurrence(anchor) <= now;
        }
        return false;
    }

    private void Advance(EnvCoordinates coords, ScheduledAction schedule, DateTime now)
        => schedules.Mutate(coords, schedule.Id, s =>
        {
            s.LastExecuted = now.ToString("o");
            if (!string.IsNullOrWhiteSpace(s.ScheduleCron) && TryCron(s.ScheduleCron!, out var cron))
                s.NextExecution = cron.GetNextOccurrence(now).ToString("o");
            else
                s.Paused = true; // one-shot: done
        });

    private static bool TryCron(string expr, out CrontabSchedule schedule)
    {
        schedule = CrontabSchedule.TryParse(expr)!;
        return schedule is not null;
    }

    private static bool TryUtc(string? value, out DateTime utc)
    {
        if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out utc))
            return true;
        utc = default;
        return false;
    }
}
