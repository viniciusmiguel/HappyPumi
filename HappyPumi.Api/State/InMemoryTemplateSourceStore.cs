#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="ITemplateSourceStore"/> (ADR-0005), keyed by (org, id). Used by unit tests.</summary>
public sealed class InMemoryTemplateSourceStore : ITemplateSourceStore
{
    private readonly ConcurrentDictionary<(string Org, string Id), StoredTemplateSource> _sources = new();

    public StoredTemplateSource Create(StoredTemplateSource source)
    {
        _sources[(source.Org, source.Id)] = source;
        return source;
    }

    public IReadOnlyList<StoredTemplateSource> List(string org)
        => _sources.Values.Where(s => s.Org == org).OrderByDescending(s => s.Created).ToArray();

    public StoredTemplateSource? Get(string org, string id)
        => _sources.TryGetValue((org, id), out var source) ? source : null;

    public StoredTemplateSource? Update(string org, string id, Action<StoredTemplateSource> mutate)
    {
        if (!_sources.TryGetValue((org, id), out var source))
            return null;
        mutate(source);
        return source;
    }

    public bool Delete(string org, string id) => _sources.TryRemove((org, id), out _);
}
