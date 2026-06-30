#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="IChangeRequestEventStore"/> (PR2, ADR-0005): append-only timeline events
/// keyed by id and scoped by Org + ChangeRequestId. <see cref="List"/> returns them oldest-first.
/// </summary>
public sealed class PostgresChangeRequestEventStore(HappyPumiDbContext db) : IChangeRequestEventStore
{
    public StoredChangeRequestEvent Append(StoredChangeRequestEvent ev)
    {
        db.ChangeRequestEvents.Add(ToRow(ev));
        db.SaveChanges();
        return ev;
    }

    public IReadOnlyList<StoredChangeRequestEvent> List(string org, string changeRequestId)
        => db.ChangeRequestEvents.AsNoTracking()
            .Where(e => e.Org == org && e.ChangeRequestId == changeRequestId)
            .OrderBy(e => e.CreatedAt).ThenBy(e => e.Id).ToList().Select(Map).ToList();

    private static ChangeRequestEventRow ToRow(StoredChangeRequestEvent e) => new()
    {
        Id = e.Id, ChangeRequestId = e.ChangeRequestId, Org = e.Org, EventType = e.EventType,
        Comment = e.Comment, RevisionNumber = e.RevisionNumber, CreatedBy = e.CreatedBy, CreatedAt = e.CreatedAt,
    };

    private static StoredChangeRequestEvent Map(ChangeRequestEventRow r) => new()
    {
        Id = r.Id, ChangeRequestId = r.ChangeRequestId, Org = r.Org, EventType = r.EventType,
        Comment = r.Comment, RevisionNumber = r.RevisionNumber, CreatedBy = r.CreatedBy, CreatedAt = r.CreatedAt,
    };
}
