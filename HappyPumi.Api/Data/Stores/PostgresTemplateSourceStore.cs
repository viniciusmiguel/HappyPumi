#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="ITemplateSourceStore"/> (templates PR1, ADR-0005): org template sources keyed
/// by id and scoped by Org. All fields are scalar columns.
/// </summary>
public sealed class PostgresTemplateSourceStore(HappyPumiDbContext db) : ITemplateSourceStore
{
    public StoredTemplateSource Create(StoredTemplateSource source)
    {
        db.TemplateSources.Add(ToRow(source));
        db.SaveChanges();
        return source;
    }

    public IReadOnlyList<StoredTemplateSource> List(string org)
        => db.TemplateSources.AsNoTracking().Where(s => s.Org == org)
            .OrderByDescending(s => s.Created).ToList().Select(Map).ToList();

    public StoredTemplateSource? Get(string org, string id)
    {
        var row = db.TemplateSources.AsNoTracking().FirstOrDefault(s => s.Org == org && s.Id == id);
        return row is null ? null : Map(row);
    }

    public StoredTemplateSource? Update(string org, string id, Action<StoredTemplateSource> mutate)
    {
        var row = db.TemplateSources.FirstOrDefault(s => s.Org == org && s.Id == id);
        if (row is null)
            return null;
        var source = Map(row);
        mutate(source);
        Apply(row, source);
        db.SaveChanges();
        return source;
    }

    public bool Delete(string org, string id)
    {
        var row = db.TemplateSources.FirstOrDefault(s => s.Org == org && s.Id == id);
        if (row is null)
            return false;
        db.TemplateSources.Remove(row);
        db.SaveChanges();
        return true;
    }

    private static TemplateSourceRow ToRow(StoredTemplateSource s)
    {
        var row = new TemplateSourceRow { Id = s.Id, Org = s.Org, Created = s.Created };
        Apply(row, s);
        return row;
    }

    private static void Apply(TemplateSourceRow row, StoredTemplateSource s)
    {
        row.Name = s.Name;
        row.SourceUrl = s.SourceUrl;
        row.DestinationUrl = s.DestinationUrl;
        row.IsValid = s.IsValid;
        row.Error = s.Error;
    }

    private static StoredTemplateSource Map(TemplateSourceRow r) => new()
    {
        Id = r.Id, Org = r.Org, Name = r.Name, SourceUrl = r.SourceUrl,
        DestinationUrl = r.DestinationUrl, IsValid = r.IsValid, Error = r.Error, Created = r.Created,
    };
}
