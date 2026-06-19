#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IPackageRegistry"/> (ADR-0005).</summary>
public sealed class PostgresPackageRegistry(HappyPumiDbContext db) : IPackageRegistry
{
    public IReadOnlyCollection<StoredPackageVersion> ListLatest(string? nameFilter)
        => db.Packages.AsNoTracking().ToList()
            .GroupBy(p => (p.Source, p.Publisher, p.Name))
            .Select(g => g.OrderByDescending(p => p.CreatedAt).First())
            .Where(p => nameFilter is null || p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
            .Select(ToStored)
            .ToList();

    public StoredPackageVersion? Get(PackageCoordinates c, string version)
    {
        if (version == "latest")
        {
            var latest = db.Packages.AsNoTracking()
                .Where(p => p.Source == c.Source && p.Publisher == c.Publisher && p.Name == c.Name)
                .OrderByDescending(p => p.CreatedAt).FirstOrDefault();
            return latest is null ? null : ToStored(latest);
        }
        var row = Row(c, version);
        return row is null ? null : ToStored(row);
    }

    public StoredPackageVersion StartPublish(PackageCoordinates c, string version, DateTime? publishedAt)
    {
        var row = Row(c, version);
        if (row is null)
        {
            row = new PackageVersionRow { Source = c.Source, Publisher = c.Publisher, Name = c.Name, Version = version };
            db.Packages.Add(row);
        }
        row.CreatedAt = DateTime.UtcNow;
        row.PublishedAt = publishedAt;
        row.Published = false;
        db.SaveChanges();
        return ToStored(row);
    }

    public bool CompletePublish(PackageCoordinates c, string version)
    {
        var row = Row(c, version);
        if (row is null)
            return false;
        row.Published = true;
        row.PublishedAt ??= DateTime.UtcNow;
        db.SaveChanges();
        return true;
    }

    public bool Delete(PackageCoordinates c, string version)
    {
        var row = Row(c, version);
        if (row is null)
            return false;
        db.Packages.Remove(row);
        db.SaveChanges();
        return true;
    }

    public IReadOnlyCollection<StoredPackageVersion> ListVersions(PackageCoordinates c)
        => db.Packages.AsNoTracking()
            .Where(p => p.Source == c.Source && p.Publisher == c.Publisher && p.Name == c.Name)
            .ToList().OrderByDescending(p => p.CreatedAt).Select(ToStored).ToList();

    private PackageVersionRow? Row(PackageCoordinates c, string version)
        => db.Packages.FirstOrDefault(p =>
            p.Source == c.Source && p.Publisher == c.Publisher && p.Name == c.Name && p.Version == version);

    private static StoredPackageVersion ToStored(PackageVersionRow r) => new()
    {
        Coordinates = new PackageCoordinates(r.Source, r.Publisher, r.Name),
        Version = r.Version, CreatedAt = r.CreatedAt, PublishedAt = r.PublishedAt, Published = r.Published,
        Readme = r.Readme, Nav = r.Nav,
    };
}
