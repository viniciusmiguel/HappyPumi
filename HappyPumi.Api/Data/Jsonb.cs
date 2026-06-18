#nullable enable

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HappyPumi.Api.Data;

/// <summary>
/// Maps a strongly-typed property to a PostgreSQL <c>jsonb</c> column by (de)serializing it with
/// System.Text.Json (ADR-0005: nested contract payloads are stored as jsonb rather than normalized).
/// </summary>
public static class Jsonb
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Configures <paramref name="property"/> as a jsonb column with a JSON value converter + comparer.</summary>
    public static PropertyBuilder<T> AsJsonb<T>(this PropertyBuilder<T> property)
    {
        var converter = new ValueConverter<T, string>(
            v => JsonSerializer.Serialize(v, Options),
            s => JsonSerializer.Deserialize<T>(s, Options)!);

        // Compare/snapshot by serialized form so EF change-tracking notices mutations to the nested object.
        var comparer = new ValueComparer<T>(
            (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
            v => v == null ? 0 : JsonSerializer.Serialize(v, Options).GetHashCode(),
            v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, Options), Options)!);

        property.HasColumnType("jsonb").HasConversion(converter, comparer);
        return property;
    }
}
