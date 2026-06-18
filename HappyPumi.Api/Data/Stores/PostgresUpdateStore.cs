#nullable enable

using System;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IUpdateStore"/> (ADR-0005).</summary>
public sealed class PostgresUpdateStore(HappyPumiDbContext db) : IUpdateStore
{
    public StoredUpdate Create(StackCoordinates stack, string kind, bool dryRun)
    {
        var update = new StoredUpdate
        {
            UpdateId = Guid.NewGuid().ToString(),
            Coordinates = stack,
            Kind = kind,
            DryRun = dryRun,
        };
        db.Updates.Add(ToRow(update));
        db.SaveChanges();
        return update;
    }

    public StoredUpdate? Find(string updateId)
    {
        var row = db.Updates.AsNoTracking().FirstOrDefault(u => u.UpdateId == updateId);
        return row is null ? null : ToStored(row);
    }

    public void Save(StoredUpdate update)
    {
        var row = db.Updates.FirstOrDefault(u => u.UpdateId == update.UpdateId);
        if (row is null)
            return;
        row.Status = update.Status;
        row.Token = update.Token;
        row.Version = update.Version;
        row.StartedAt = update.StartedAt;
        row.Message = update.Message;
        row.Config = update.Config;
        row.Checkpoint = update.Checkpoint;
        db.SaveChanges();
    }

    private static UpdateRow ToRow(StoredUpdate u) => new()
    {
        UpdateId = u.UpdateId,
        Org = u.Coordinates.Org, Project = u.Coordinates.Project, Stack = u.Coordinates.Stack,
        Kind = u.Kind, DryRun = u.DryRun, Status = u.Status, Token = u.Token,
        Version = u.Version, StartedAt = u.StartedAt, Message = u.Message,
        Config = u.Config, Checkpoint = u.Checkpoint,
    };

    private static StoredUpdate ToStored(UpdateRow r) => new()
    {
        UpdateId = r.UpdateId,
        Coordinates = new StackCoordinates(r.Org, r.Project, r.Stack),
        Kind = r.Kind, DryRun = r.DryRun,
        Status = r.Status, Token = r.Token, Version = r.Version,
        StartedAt = r.StartedAt, Message = r.Message, Config = r.Config, Checkpoint = r.Checkpoint,
    };
}
