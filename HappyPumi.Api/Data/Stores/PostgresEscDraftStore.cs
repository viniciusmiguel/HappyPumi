#nullable enable

using System;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL <see cref="IEscDraftStore"/>: environment drafts, payload as jsonb.</summary>
public sealed class PostgresEscDraftStore(HappyPumiDbContext db) : IEscDraftStore
{
    public string Create(EnvCoordinates e, string yaml, long baseRevision)
    {
        var id = Guid.NewGuid().ToString("N");
        db.EnvironmentDrafts.Add(new EnvironmentDraftRow
        {
            Id = id, Org = e.Org, Project = e.Project, Name = e.Name, Draft = new EscDraft(id, yaml, baseRevision),
        });
        db.SaveChanges();
        return id;
    }

    public EscDraft? Get(EnvCoordinates e, string changeRequestId) => Row(e, changeRequestId)?.Draft;

    public bool Update(EnvCoordinates e, string changeRequestId, string yaml)
    {
        var row = Row(e, changeRequestId);
        if (row is null)
            return false;
        row.Draft = row.Draft with { Yaml = yaml };
        db.SaveChanges();
        return true;
    }

    private EnvironmentDraftRow? Row(EnvCoordinates e, string id)
        => db.EnvironmentDrafts.FirstOrDefault(r => r.Id == id && r.Org == e.Org && r.Project == e.Project && r.Name == e.Name);
}
