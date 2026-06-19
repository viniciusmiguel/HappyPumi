#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL <see cref="IEscScheduleStore"/>: environment scheduled actions, payload as jsonb.</summary>
public sealed class PostgresEscScheduleStore(HappyPumiDbContext db) : IEscScheduleStore
{
    public ScheduledAction Add(EnvCoordinates e, ScheduledAction action)
    {
        db.EnvironmentSchedules.Add(new EnvironmentScheduleRow
        {
            Id = action.Id, Org = e.Org, Project = e.Project, Name = e.Name, Created = DateTime.UtcNow, Action = action,
        });
        db.SaveChanges();
        return action;
    }

    public IReadOnlyList<ScheduledAction> List(EnvCoordinates e)
        => db.EnvironmentSchedules.AsNoTracking()
            .Where(r => r.Org == e.Org && r.Project == e.Project && r.Name == e.Name)
            .ToList().OrderBy(r => r.Created).Select(r => r.Action).ToList();

    public ScheduledAction? Get(EnvCoordinates e, string scheduleId) => Row(e, scheduleId)?.Action;

    public bool Remove(EnvCoordinates e, string scheduleId)
    {
        var row = Row(e, scheduleId);
        if (row is null)
            return false;
        db.EnvironmentSchedules.Remove(row);
        db.SaveChanges();
        return true;
    }

    public ScheduledAction? Mutate(EnvCoordinates e, string scheduleId, Action<ScheduledAction> edit)
    {
        var row = Row(e, scheduleId);
        if (row is null)
            return null;
        edit(row.Action);
        row.Action.Modified = DateTime.UtcNow.ToString("o");
        db.SaveChanges(); // the jsonb value comparer snapshots by serialized form, so the edit is detected
        return row.Action;
    }

    private EnvironmentScheduleRow? Row(EnvCoordinates e, string id)
        => db.EnvironmentSchedules.FirstOrDefault(r => r.Id == id && r.Org == e.Org && r.Project == e.Project && r.Name == e.Name);
}
