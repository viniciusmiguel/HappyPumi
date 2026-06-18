#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IPackageRegistry"/> (ADR-0005), keyed by coordinates then version.</summary>
public sealed class InMemoryPackageRegistry : IPackageRegistry
{
    private readonly ConcurrentDictionary<PackageCoordinates, ConcurrentDictionary<string, StoredPackageVersion>> _packages = new();

    private ConcurrentDictionary<string, StoredPackageVersion> Versions(PackageCoordinates c)
        => _packages.GetOrAdd(c, _ => new ConcurrentDictionary<string, StoredPackageVersion>());

    public IReadOnlyCollection<StoredPackageVersion> ListLatest(string? nameFilter)
    {
        return _packages.Values
            .Select(versions => versions.Values
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefault())
            .OfType<StoredPackageVersion>()
            .Where(v => nameFilter is null ||
                        v.Coordinates.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public StoredPackageVersion? Get(PackageCoordinates coordinates, string version)
    {
        if (version == "latest")
            return Versions(coordinates).Values.OrderByDescending(v => v.CreatedAt).FirstOrDefault();
        return Versions(coordinates).TryGetValue(version, out var v) ? v : null;
    }

    public StoredPackageVersion StartPublish(PackageCoordinates coordinates, string version, DateTime? publishedAt)
    {
        var entry = new StoredPackageVersion
        {
            Coordinates = coordinates,
            Version = version,
            PublishedAt = publishedAt,
            Published = false,
        };
        Versions(coordinates)[version] = entry;
        return entry;
    }

    public bool CompletePublish(PackageCoordinates coordinates, string version)
    {
        if (!Versions(coordinates).TryGetValue(version, out var entry))
            return false;
        entry.Published = true;
        entry.PublishedAt ??= DateTime.UtcNow;
        return true;
    }

    public bool Delete(PackageCoordinates coordinates, string version)
        => Versions(coordinates).TryRemove(version, out _);
}
