#nullable enable

using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL <see cref="IArtifactStore"/>: registry artifact bytes keyed by logical path.</summary>
public sealed class PostgresArtifactStore(HappyPumiDbContext db) : IArtifactStore
{
    public void Put(string key, byte[] content, string contentType)
    {
        var row = db.RegistryArtifacts.FirstOrDefault(a => a.Key == key);
        if (row is null)
            db.RegistryArtifacts.Add(new RegistryArtifactRow { Key = key, Content = content, ContentType = contentType });
        else
        {
            row.Content = content;
            row.ContentType = contentType;
        }
        db.SaveChanges();
    }

    public StoredArtifact? Get(string key)
    {
        var row = db.RegistryArtifacts.AsNoTracking().FirstOrDefault(a => a.Key == key);
        return row is null ? null : new StoredArtifact(row.Content, row.ContentType);
    }

    public bool Exists(string key) => db.RegistryArtifacts.AsNoTracking().Any(a => a.Key == key);
}
