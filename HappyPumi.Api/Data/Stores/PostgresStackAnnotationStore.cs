#nullable enable

using System.Linq;
using System.Text.Json;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="IStackAnnotationStore"/> (ADR-0005). The arbitrary payload is held as a
/// jsonb string column and round-tripped through <see cref="JsonElement"/> so any JSON shape is preserved.
/// </summary>
public sealed class PostgresStackAnnotationStore(HappyPumiDbContext db) : IStackAnnotationStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public object? Get(StackCoordinates c, string kind)
    {
        var row = Row(c, kind);
        return row is null ? null : JsonSerializer.Deserialize<JsonElement>(row.Payload, Options);
    }

    public void Set(StackCoordinates c, string kind, object payload)
    {
        var json = JsonSerializer.Serialize(payload, Options);
        var row = Row(c, kind);
        if (row is null)
        {
            db.StackAnnotations.Add(new StackAnnotationRow
            {
                Org = c.Org, Project = c.Project, Stack = c.Stack, Kind = kind, Payload = json,
            });
        }
        else
        {
            row.Payload = json;
        }
        db.SaveChanges();
    }

    private StackAnnotationRow? Row(StackCoordinates c, string kind)
        => db.StackAnnotations.FirstOrDefault(a =>
            a.Org == c.Org && a.Project == c.Project && a.Stack == c.Stack && a.Kind == kind);
}
