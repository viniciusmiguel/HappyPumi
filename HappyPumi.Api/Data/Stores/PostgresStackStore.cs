#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IStackStore"/> (ADR-0005), replacing the in-memory default.</summary>
public sealed class PostgresStackStore(HappyPumiDbContext db) : IStackStore
{
    public bool ProjectExists(string org, string project)
        => db.Stacks.Any(s => s.Org == org && s.Project == project);

    public StoredStack? Find(StackCoordinates c)
    {
        var row = db.Stacks.FirstOrDefault(s => s.Org == c.Org && s.Project == c.Project && s.Stack == c.Stack);
        return row is null ? null : ToStored(row, LoadHistory(c));
    }

    public bool TryCreate(StoredStack stack)
    {
        var c = stack.Coordinates;
        if (db.Stacks.Any(s => s.Org == c.Org && s.Project == c.Project && s.Stack == c.Stack))
            return false;
        db.Stacks.Add(new StackRow
        {
            Org = c.Org, Project = c.Project, Stack = c.Stack,
            Version = stack.Version,
            Tags = new Dictionary<string, string>(stack.Tags),
            Config = stack.Config,
            Deployment = stack.Deployment,
        });
        db.SaveChanges();
        return true;
    }

    public bool Delete(StackCoordinates c)
    {
        var row = Row(c);
        if (row is null)
            return false;
        db.Stacks.Remove(row);
        db.StackUpdates.RemoveRange(db.StackUpdates.Where(h => h.Org == c.Org && h.Project == c.Project && h.Stack == c.Stack));
        db.SaveChanges();
        return true;
    }

    public StoredStack? SetConfig(StackCoordinates c, AppStackConfig config)
    {
        var row = Row(c);
        if (row is null)
            return null;
        row.Config = config;
        db.SaveChanges();
        return ToStored(row, LoadHistory(c));
    }

    public bool ClearConfig(StackCoordinates c)
    {
        var row = Row(c);
        if (row is null)
            return false;
        row.Config = null;
        db.SaveChanges();
        return true;
    }

    public StoredStack? SetDeployment(StackCoordinates c, AppUntypedDeployment deployment, bool bumpVersion)
    {
        var row = Row(c);
        if (row is null)
            return null;
        row.Deployment = deployment;
        if (bumpVersion)
            row.Version++;
        db.SaveChanges();
        return ToStored(row, LoadHistory(c));
    }

    public IReadOnlyCollection<StoredStack> All()
        => db.Stacks.AsNoTracking().ToList()
            .Select(r => ToStored(r, LoadHistory(new StackCoordinates(r.Org, r.Project, r.Stack))))
            .ToList();

    public bool RecordHistory(StackCoordinates c, StoredHistoryEntry entry)
    {
        if (Row(c) is null)
            return false;
        var i = entry.Info;
        db.StackUpdates.Add(new StackUpdateRow
        {
            UpdateId = entry.UpdateId, Org = c.Org, Project = c.Project, Stack = c.Stack,
            Version = i.Version, Kind = i.Kind, Result = i.Result, Message = i.Message ?? string.Empty,
            StartTime = i.StartTime, EndTime = i.EndTime,
            RequestedByLogin = entry.RequestedByLogin, RequestedByName = entry.RequestedByName,
            Config = i.Config ?? new Dictionary<string, AppConfigValue>(),
        });
        db.SaveChanges();
        return true;
    }

    public StoredStack? SetTag(StackCoordinates c, string name, string value)
    {
        var row = Row(c);
        if (row is null)
            return null;
        row.Tags[name] = value;
        db.Entry(row).Property(r => r.Tags).IsModified = true; // mutating the dictionary in place
        db.SaveChanges();
        return ToStored(row, LoadHistory(c));
    }

    public StoredStack? ReplaceTags(StackCoordinates c, IReadOnlyDictionary<string, string> tags)
    {
        var row = Row(c);
        if (row is null)
            return null;
        row.Tags = new Dictionary<string, string>(tags);
        db.SaveChanges();
        return ToStored(row, LoadHistory(c));
    }

    public (StoredStack? Stack, bool TagExisted) RemoveTag(StackCoordinates c, string name)
    {
        var row = Row(c);
        if (row is null)
            return (null, false);
        if (!row.Tags.Remove(name))
            return (ToStored(row, LoadHistory(c)), false);
        db.Entry(row).Property(r => r.Tags).IsModified = true; // mutating the dictionary in place
        db.SaveChanges();
        return (ToStored(row, LoadHistory(c)), true);
    }

    public StoredStack? Rename(StackCoordinates from, StackCoordinates to, out bool collision)
    {
        collision = false;
        var src = Row(from);
        if (src is null)
            return null;
        if (from == to)
            return ToStored(src, LoadHistory(from));
        if (Row(to) is not null)
        {
            collision = true;
            return null;
        }

        // Re-key the stack (composite PK can't be updated in place) and move its history rows.
        db.Stacks.Add(new StackRow
        {
            Org = to.Org, Project = to.Project, Stack = to.Stack,
            Version = src.Version, Tags = new Dictionary<string, string>(src.Tags),
            Config = src.Config, Deployment = src.Deployment,
        });
        db.Stacks.Remove(src);
        foreach (var h in db.StackUpdates.Where(h => h.Org == from.Org && h.Project == from.Project && h.Stack == from.Stack))
        {
            h.Org = to.Org; h.Project = to.Project; h.Stack = to.Stack;
        }
        db.SaveChanges();
        return Find(to);
    }

    private StackRow? Row(StackCoordinates c)
        => db.Stacks.FirstOrDefault(s => s.Org == c.Org && s.Project == c.Project && s.Stack == c.Stack);

    private List<StackUpdateRow> LoadHistory(StackCoordinates c)
        => db.StackUpdates.AsNoTracking()
            .Where(h => h.Org == c.Org && h.Project == c.Project && h.Stack == c.Stack)
            .OrderBy(h => h.Version)
            .ToList();

    private static StoredStack ToStored(StackRow row, List<StackUpdateRow> history)
    {
        var stack = new StoredStack
        {
            Coordinates = new StackCoordinates(row.Org, row.Project, row.Stack),
            Version = row.Version,
            Config = row.Config,
            Deployment = row.Deployment,
        };
        foreach (var (k, v) in row.Tags)
            stack.Tags[k] = v;
        foreach (var h in history)
            stack.History.Add(new StoredHistoryEntry
            {
                UpdateId = h.UpdateId, Info = ToInfo(h),
                RequestedByLogin = h.RequestedByLogin, RequestedByName = h.RequestedByName,
            });
        return stack;
    }

    private static AppUpdateInfo ToInfo(StackUpdateRow h) => new()
    {
        Kind = h.Kind, Result = h.Result, Message = h.Message,
        StartTime = h.StartTime, EndTime = h.EndTime, Version = h.Version,
        Config = h.Config, Environment = new Dictionary<string, string>(),
    };
}
