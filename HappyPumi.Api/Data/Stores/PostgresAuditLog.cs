#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IAuditLog"/> (ADR-0010).</summary>
public sealed class PostgresAuditLog(HappyPumiDbContext db) : IAuditLog
{
    public void Record(string org, string @event, string description, string actor)
    {
        db.AuditLogs.Add(new AuditLogRow
        {
            Org = org, Event = @event, Description = description, ActorName = actor, Timestamp = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    public IReadOnlyList<AuditLogRow> List(string org)
        => db.AuditLogs.AsNoTracking().Where(a => a.Org == org).OrderByDescending(a => a.Id).ToList();
}
