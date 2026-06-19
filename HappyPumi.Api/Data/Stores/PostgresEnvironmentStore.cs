#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL <see cref="IEnvironmentStore"/>: environment definitions + revision history.</summary>
public sealed class PostgresEnvironmentStore(HappyPumiDbContext db) : IEnvironmentStore
{
    public IReadOnlyList<StoredEnvironment> ListByOrg(string org)
        => db.Environments.AsNoTracking().Where(e => e.Org == org && !e.Deleted)
            .ToList().OrderBy(e => e.Project).ThenBy(e => e.Name).Select(Map).ToList();

    public IReadOnlyList<StoredEnvironment> ListAll()
        => db.Environments.AsNoTracking().Where(e => !e.Deleted)
            .ToList().OrderBy(e => e.Org).ThenBy(e => e.Project).ThenBy(e => e.Name).Select(Map).ToList();

    public StoredEnvironment? Get(EnvCoordinates c)
    {
        var row = Row(c);
        return row is null ? null : Map(row);
    }

    public StoredEnvironment? Create(EnvCoordinates c, string ownerLogin, string ownerName)
    {
        // Block creation while any row (even a soft-deleted one) holds the name; restore it instead.
        if (RowAny(c) is not null)
            return null;
        var now = DateTime.UtcNow;
        var row = new EnvironmentRow
        {
            Org = c.Org, Project = c.Project, Name = c.Name, Created = now, Modified = now,
            OwnerLogin = ownerLogin, OwnerName = ownerName, Yaml = "values: {}\n", CurrentRevision = 1,
        };
        db.Environments.Add(row);
        db.EnvironmentRevisions.Add(NewRevision(c, 1, row.Yaml, ownerLogin, ownerName, new List<string> { "latest" }));
        db.SaveChanges();
        return Map(row);
    }

    public StoredEnvironment? UpdateYaml(EnvCoordinates c, string yaml, string editorLogin, string editorName)
    {
        var row = Row(c);
        if (row is null)
            return null;
        row.Yaml = yaml;
        row.Modified = DateTime.UtcNow;
        row.CurrentRevision += 1;
        db.EnvironmentRevisions.Add(NewRevision(c, row.CurrentRevision, yaml, editorLogin, editorName, new List<string> { "latest" }));
        db.SaveChanges();
        return Map(row);
    }

    public IReadOnlyList<StoredEnvRevision> ListRevisions(EnvCoordinates c)
        => db.EnvironmentRevisions.AsNoTracking()
            .Where(r => r.Org == c.Org && r.Project == c.Project && r.Name == c.Name)
            .ToList().OrderByDescending(r => r.Number)
            .Select(MapRevision).ToList();

    // Soft delete: hide the environment but keep its rows so it can be restored within the retention window.
    public bool Delete(EnvCoordinates c)
    {
        var row = Row(c);
        if (row is null)
            return false;
        row.Deleted = true;
        row.DeletedAt = DateTime.UtcNow;
        db.SaveChanges();
        return true;
    }

    public StoredEnvironment? Restore(EnvCoordinates c)
    {
        var row = RowAny(c);
        if (row is null || !row.Deleted)
            return null;
        row.Deleted = false;
        row.DeletedAt = null;
        db.SaveChanges();
        return Map(row);
    }

    public StoredEnvironment? SetDeletionProtected(EnvCoordinates c, bool deletionProtected)
        => Mutate(c, row => row.DeletionProtected = deletionProtected);

    public StoredEnvironment? ReassignOwner(EnvCoordinates c, string ownerLogin, string ownerName)
        => Mutate(c, row => { row.OwnerLogin = ownerLogin; row.OwnerName = ownerName; });

    // Reassign a new dictionary (not in-place) so EF's jsonb change tracking always detects the edit.
    public StoredEnvironment? SetTag(EnvCoordinates c, string name, string value)
        => Mutate(c, row => row.Tags = new Dictionary<string, string>(row.Tags) { [name] = value });

    public bool DeleteTag(EnvCoordinates c, string name)
    {
        var row = Row(c);
        if (row is null || !row.Tags.ContainsKey(name))
            return false;
        var tags = new Dictionary<string, string>(row.Tags);
        tags.Remove(name);
        row.Tags = tags;
        db.SaveChanges();
        return true;
    }

    // Applies an edit to the environment row and persists it; null when the environment does not exist.
    private StoredEnvironment? Mutate(EnvCoordinates c, Action<EnvironmentRow> edit)
    {
        var row = Row(c);
        if (row is null)
            return null;
        edit(row);
        row.Modified = DateTime.UtcNow;
        db.SaveChanges();
        return Map(row);
    }

    public StoredEnvRevision? SetRevisionTag(EnvCoordinates c, string name, long revision)
    {
        var rows = RevisionRows(c);
        var target = rows.FirstOrDefault(r => r.Number == revision);
        if (target is null)
            return null;
        foreach (var row in rows.Where(r => r.Tags.Contains(name)))
            row.Tags = Without(row.Tags, name);
        if (!target.Tags.Contains(name))
            target.Tags = new List<string>(target.Tags) { name };
        db.SaveChanges();
        return MapRevision(target);
    }

    public bool DeleteRevisionTag(EnvCoordinates c, string name)
    {
        var owner = RevisionRows(c).FirstOrDefault(r => r.Tags.Contains(name));
        if (owner is null)
            return false;
        owner.Tags = Without(owner.Tags, name);
        db.SaveChanges();
        return true;
    }

    public StoredEnvRevision? RetractRevision(EnvCoordinates c, long version, string? reason, long? replacement,
        string byLogin, string byName)
    {
        var row = RevisionRows(c).FirstOrDefault(r => r.Number == version);
        if (row is null)
            return null;
        row.Retracted = true;
        row.RetractedAt = DateTime.UtcNow;
        row.RetractedByLogin = byLogin;
        row.RetractedByName = byName;
        row.RetractReason = reason;
        row.RetractReplacement = replacement;
        db.SaveChanges();
        return MapRevision(row);
    }

    private List<EnvironmentRevisionRow> RevisionRows(EnvCoordinates c)
        => db.EnvironmentRevisions.Where(r => r.Org == c.Org && r.Project == c.Project && r.Name == c.Name).ToList();

    // Reassign a new list (not in-place) so EF's jsonb change tracking detects the edit.
    private static List<string> Without(List<string> tags, string name)
    {
        var copy = new List<string>(tags);
        copy.Remove(name);
        return copy;
    }

    private static StoredEnvRevision MapRevision(EnvironmentRevisionRow r) => new()
    {
        Number = r.Number, Created = r.Created, CreatorLogin = r.CreatorLogin,
        CreatorName = r.CreatorName, Yaml = r.Yaml, Tags = r.Tags,
        Retracted = r.Retracted, RetractedAt = r.RetractedAt, RetractedByLogin = r.RetractedByLogin,
        RetractedByName = r.RetractedByName, RetractReason = r.RetractReason, RetractReplacement = r.RetractReplacement,
    };

    // Active (non-deleted) row — the default for reads and edits.
    private EnvironmentRow? Row(EnvCoordinates c)
        => db.Environments.FirstOrDefault(e => e.Org == c.Org && e.Project == c.Project && e.Name == c.Name && !e.Deleted);

    // Any row including soft-deleted — used by create (name reservation) and restore.
    private EnvironmentRow? RowAny(EnvCoordinates c)
        => db.Environments.FirstOrDefault(e => e.Org == c.Org && e.Project == c.Project && e.Name == c.Name);

    private static EnvironmentRevisionRow NewRevision(EnvCoordinates c, long number, string yaml,
        string login, string name, List<string> tags) => new()
    {
        Id = Guid.NewGuid().ToString(), Org = c.Org, Project = c.Project, Name = c.Name, Number = number,
        Created = DateTime.UtcNow, CreatorLogin = login, CreatorName = name, Yaml = yaml, Tags = tags,
    };

    private static StoredEnvironment Map(EnvironmentRow e) => new()
    {
        Coordinates = new EnvCoordinates(e.Org, e.Project, e.Name),
        Created = e.Created, Modified = e.Modified, OwnerLogin = e.OwnerLogin, OwnerName = e.OwnerName,
        DeletionProtected = e.DeletionProtected, Yaml = e.Yaml, CurrentRevision = e.CurrentRevision, Tags = e.Tags,
        Deleted = e.Deleted, DeletedAt = e.DeletedAt,
    };
}
