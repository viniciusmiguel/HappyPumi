#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="IChangeRequestStore"/> (PR2, ADR-0005): change requests keyed by id and
/// scoped by Org. The approver list round-trips through a jsonb column.
/// </summary>
public sealed class PostgresChangeRequestStore(HappyPumiDbContext db) : IChangeRequestStore
{
    public StoredChangeRequest Create(StoredChangeRequest cr)
    {
        db.ChangeRequests.Add(ToRow(cr));
        db.SaveChanges();
        return cr;
    }

    public IReadOnlyList<StoredChangeRequest> List(string org)
        => db.ChangeRequests.AsNoTracking().Where(c => c.Org == org)
            .OrderByDescending(c => c.CreatedAt).ToList().Select(Map).ToList();

    public StoredChangeRequest? Get(string org, string id)
    {
        var row = db.ChangeRequests.AsNoTracking().FirstOrDefault(c => c.Org == org && c.Id == id);
        return row is null ? null : Map(row);
    }

    public StoredChangeRequest? Update(string org, string id, Action<StoredChangeRequest> mutate)
    {
        var row = db.ChangeRequests.FirstOrDefault(c => c.Org == org && c.Id == id);
        if (row is null)
            return null;
        var cr = Map(row);
        mutate(cr);
        Apply(row, cr);
        db.SaveChanges();
        return cr;
    }

    private static ChangeRequestRow ToRow(StoredChangeRequest c)
    {
        var row = new ChangeRequestRow { Id = c.Id, Org = c.Org, CreatedAt = c.CreatedAt, CreatedBy = c.CreatedBy };
        Apply(row, c);
        return row;
    }

    private static void Apply(ChangeRequestRow row, StoredChangeRequest c)
    {
        row.Action = c.Action;
        row.Description = c.Description;
        row.TargetProject = c.TargetProject;
        row.TargetEnv = c.TargetEnv;
        row.Status = c.Status;
        row.LatestRevisionNumber = c.LatestRevisionNumber;
        row.CreatedBy = c.CreatedBy;
        row.Approvers = c.Approvers.ToList();
    }

    private static StoredChangeRequest Map(ChangeRequestRow r) => new()
    {
        Id = r.Id, Org = r.Org, Action = r.Action, Description = r.Description,
        TargetProject = r.TargetProject, TargetEnv = r.TargetEnv, Status = r.Status,
        LatestRevisionNumber = r.LatestRevisionNumber, CreatedBy = r.CreatedBy,
        CreatedAt = r.CreatedAt, Approvers = r.Approvers.ToList(),
    };
}
