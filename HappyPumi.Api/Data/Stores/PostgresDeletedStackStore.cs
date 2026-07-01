#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="IDeletedStackStore"/> (org-admin PR5, ADR-0005): deleted-stack tombstones
/// keyed by program id and scoped by Org.
/// </summary>
public sealed class PostgresDeletedStackStore(HappyPumiDbContext db) : IDeletedStackStore
{
    public StoredDeletedStack Record(StoredDeletedStack deleted)
    {
        db.DeletedStacks.Add(ToRow(deleted));
        db.SaveChanges();
        return deleted;
    }

    public IReadOnlyList<StoredDeletedStack> List(string org)
        => db.DeletedStacks.AsNoTracking().Where(t => t.Org == org)
            .OrderByDescending(t => t.DeletedAtUnix).ToList().Select(Map).ToList();

    public StoredDeletedStack? Get(string org, string programId)
    {
        var row = db.DeletedStacks.AsNoTracking().FirstOrDefault(t => t.Org == org && t.ProgramId == programId);
        return row is null ? null : Map(row);
    }

    public bool Remove(string org, string programId)
    {
        var row = db.DeletedStacks.FirstOrDefault(t => t.Org == org && t.ProgramId == programId);
        if (row is null)
            return false;
        db.DeletedStacks.Remove(row);
        db.SaveChanges();
        return true;
    }

    private static DeletedStackRow ToRow(StoredDeletedStack t) => new()
    {
        Id = t.Id, Org = t.Org, ProjectName = t.ProjectName, StackName = t.StackName,
        ProgramId = t.ProgramId, Version = t.Version, DeletedAtUnix = t.DeletedAtUnix,
    };

    private static StoredDeletedStack Map(DeletedStackRow r) => new()
    {
        Id = r.Id, Org = r.Org, ProjectName = r.ProjectName, StackName = r.StackName,
        ProgramId = r.ProgramId, Version = r.Version, DeletedAtUnix = r.DeletedAtUnix,
    };
}
