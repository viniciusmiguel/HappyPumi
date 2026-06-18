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
        => db.Environments.AsNoTracking().Where(e => e.Org == org)
            .ToList().OrderBy(e => e.Project).ThenBy(e => e.Name).Select(Map).ToList();

    public StoredEnvironment? Get(EnvCoordinates c)
    {
        var row = Row(c);
        return row is null ? null : Map(row);
    }

    public StoredEnvironment? Create(EnvCoordinates c, string ownerLogin, string ownerName)
    {
        if (Row(c) is not null)
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
            .Select(r => new StoredEnvRevision
            {
                Number = r.Number, Created = r.Created, CreatorLogin = r.CreatorLogin,
                CreatorName = r.CreatorName, Yaml = r.Yaml, Tags = r.Tags,
            }).ToList();

    public bool Delete(EnvCoordinates c)
    {
        var row = Row(c);
        if (row is null)
            return false;
        db.Environments.Remove(row);
        var revisions = db.EnvironmentRevisions.Where(r => r.Org == c.Org && r.Project == c.Project && r.Name == c.Name);
        db.EnvironmentRevisions.RemoveRange(revisions);
        db.SaveChanges();
        return true;
    }

    private EnvironmentRow? Row(EnvCoordinates c)
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
    };
}
