#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for the package registry (ENDPOINTS.md 4), in-memory by default (ADR-0005). Models
/// the two-step publish handshake (start → upload → complete). Safe for concurrent use.
/// </summary>
public interface IPackageRegistry
{
    /// <summary>The latest version of each package, optionally filtered by name (substring, case-insensitive).</summary>
    IReadOnlyCollection<StoredPackageVersion> ListLatest(string? nameFilter);
    /// <summary>All versions of a package, newest first (the Versions tab + version selector).</summary>
    IReadOnlyCollection<StoredPackageVersion> ListVersions(PackageCoordinates coordinates);

    /// <summary>Gets a specific version, or the most recent when <paramref name="version"/> is "latest".</summary>
    StoredPackageVersion? Get(PackageCoordinates coordinates, string version);

    /// <summary>Begins publishing a version (created unpublished until <see cref="CompletePublish"/>).</summary>
    StoredPackageVersion StartPublish(PackageCoordinates coordinates, string version, DateTime? publishedAt);

    /// <summary>Finalizes a started publish. Returns false when no such pending/version exists.</summary>
    bool CompletePublish(PackageCoordinates coordinates, string version);

    bool Delete(PackageCoordinates coordinates, string version);
}
