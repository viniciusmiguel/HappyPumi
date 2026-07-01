#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// A persisted org template source (templates PR1, ADR-0005). A source records where project templates are
/// fetched from (<see cref="SourceUrl"/>) plus an optional publish destination. <see cref="IsValid"/> /
/// <see cref="Error"/> carry the result of the deterministic URL validation run on create/update.
/// </summary>
public sealed class StoredTemplateSource
{
    public required string Id { get; init; }
    public required string Org { get; init; }
    public string Name { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string? DestinationUrl { get; set; }
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persistence seam for org template sources (ADR-0005). Backed by PostgreSQL in production and an in-memory
/// map in unit tests. Sources are keyed by id within an org. <see cref="Update"/> mutates in place and
/// returns the updated record (null when missing), mirroring the other settings-cluster stores.
/// </summary>
public interface ITemplateSourceStore
{
    /// <summary>Persists a new source and returns it.</summary>
    StoredTemplateSource Create(StoredTemplateSource source);

    /// <summary>All sources for an org, newest first.</summary>
    IReadOnlyList<StoredTemplateSource> List(string org);

    /// <summary>A single source by id within an org, or null when missing.</summary>
    StoredTemplateSource? Get(string org, string id);

    /// <summary>Applies <paramref name="mutate"/> to the source and returns it; null when missing.</summary>
    StoredTemplateSource? Update(string org, string id, Action<StoredTemplateSource> mutate);

    /// <summary>Removes a source. False when it does not exist.</summary>
    bool Delete(string org, string id);
}
