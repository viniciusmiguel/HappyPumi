#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="ITemplateRegistry"/> (ADR-0005), keyed by coordinates then version.</summary>
public sealed class InMemoryTemplateRegistry : ITemplateRegistry
{
    private readonly ConcurrentDictionary<TemplateCoordinates, ConcurrentDictionary<string, StoredTemplateVersion>> _templates = new();

    private ConcurrentDictionary<string, StoredTemplateVersion> Versions(TemplateCoordinates c)
        => _templates.GetOrAdd(c, _ => new ConcurrentDictionary<string, StoredTemplateVersion>());

    public IReadOnlyCollection<StoredTemplateVersion> ListLatest(string? nameFilter)
    {
        return _templates.Values
            .Select(versions => versions.Values.OrderByDescending(v => v.UpdatedAt).FirstOrDefault())
            .OfType<StoredTemplateVersion>()
            .Where(v => nameFilter is null ||
                        v.Coordinates.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<StoredTemplateVersion> ListVersions(TemplateCoordinates coordinates)
        => Versions(coordinates).Values.OrderByDescending(v => v.UpdatedAt).ToArray();

    public StoredTemplateVersion? Get(TemplateCoordinates coordinates, string version)
    {
        if (version == "latest")
            return Versions(coordinates).Values.OrderByDescending(v => v.UpdatedAt).FirstOrDefault();
        return Versions(coordinates).TryGetValue(version, out var v) ? v : null;
    }

    public StoredTemplateVersion StartPublish(TemplateCoordinates coordinates, string version)
    {
        var entry = new StoredTemplateVersion { Coordinates = coordinates, Version = version, Published = false };
        Versions(coordinates)[version] = entry;
        return entry;
    }

    public bool CompletePublish(TemplateCoordinates coordinates, string version)
    {
        if (!Versions(coordinates).TryGetValue(version, out var entry))
            return false;
        entry.Published = true;
        entry.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public bool Delete(TemplateCoordinates coordinates, string version)
        => Versions(coordinates).TryRemove(version, out _);
}
