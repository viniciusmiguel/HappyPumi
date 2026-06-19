#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="ITemplateRegistry"/> (ADR-0005).</summary>
public sealed class PostgresTemplateRegistry(HappyPumiDbContext db) : ITemplateRegistry
{
    public IReadOnlyCollection<StoredTemplateVersion> ListLatest(string? nameFilter)
        => db.Templates.AsNoTracking().ToList()
            .GroupBy(t => (t.Source, t.Publisher, t.Name))
            .Select(g => g.OrderByDescending(t => t.UpdatedAt).First())
            .Where(t => nameFilter is null || t.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
            .Select(ToStored)
            .ToList();

    public IReadOnlyList<StoredTemplateVersion> ListVersions(TemplateCoordinates c)
        => db.Templates.AsNoTracking()
            .Where(t => t.Source == c.Source && t.Publisher == c.Publisher && t.Name == c.Name)
            .OrderByDescending(t => t.UpdatedAt).ToList()
            .Select(ToStored).ToList();

    public StoredTemplateVersion? Get(TemplateCoordinates c, string version)
    {
        if (version == "latest")
        {
            var latest = db.Templates.AsNoTracking()
                .Where(t => t.Source == c.Source && t.Publisher == c.Publisher && t.Name == c.Name)
                .OrderByDescending(t => t.UpdatedAt).FirstOrDefault();
            return latest is null ? null : ToStored(latest);
        }
        var row = Row(c, version);
        return row is null ? null : ToStored(row);
    }

    public StoredTemplateVersion StartPublish(TemplateCoordinates c, string version)
    {
        var row = Row(c, version);
        if (row is null)
        {
            row = new TemplateVersionRow { Source = c.Source, Publisher = c.Publisher, Name = c.Name, Version = version };
            db.Templates.Add(row);
        }
        row.UpdatedAt = DateTime.UtcNow;
        row.Published = false;
        db.SaveChanges();
        return ToStored(row);
    }

    public bool CompletePublish(TemplateCoordinates c, string version)
    {
        var row = Row(c, version);
        if (row is null)
            return false;
        row.Published = true;
        row.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();
        return true;
    }

    public bool Delete(TemplateCoordinates c, string version)
    {
        var row = Row(c, version);
        if (row is null)
            return false;
        db.Templates.Remove(row);
        db.SaveChanges();
        return true;
    }

    private TemplateVersionRow? Row(TemplateCoordinates c, string version)
        => db.Templates.FirstOrDefault(t =>
            t.Source == c.Source && t.Publisher == c.Publisher && t.Name == c.Name && t.Version == version);

    private static StoredTemplateVersion ToStored(TemplateVersionRow r) => new()
    {
        Coordinates = new TemplateCoordinates(r.Source, r.Publisher, r.Name),
        Version = r.Version, UpdatedAt = r.UpdatedAt, Language = r.Language,
        Description = r.Description, Published = r.Published,
    };
}
