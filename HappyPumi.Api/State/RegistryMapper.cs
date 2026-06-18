#nullable enable

using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>Maps registry domain records to their wire DTOs.</summary>
public static class RegistryMapper
{
    public static PackageMetadata ToMetadata(StoredPackageVersion pkg) => new()
    {
        Source = pkg.Coordinates.Source,
        Publisher = pkg.Coordinates.Publisher,
        Name = pkg.Coordinates.Name,
        Version = pkg.Version,
        CreatedAt = pkg.CreatedAt,
        PackageStatus = pkg.Published ? "published" : "pending",
        Visibility = "public",
        // Artifact URLs are not served by this in-memory registry; advertise canonical (stub) locations.
        SchemaUrl = $"/api/registry/packages/{pkg.Coordinates.Source}/{pkg.Coordinates.Publisher}/{pkg.Coordinates.Name}/versions/{pkg.Version}/schema",
        ReadmeUrl = $"/api/registry/packages/{pkg.Coordinates.Source}/{pkg.Coordinates.Publisher}/{pkg.Coordinates.Name}/versions/{pkg.Version}/readme",
    };

    /// <summary>Stub upload destinations for the publish handshake (no real blob storage yet).</summary>
    public static PackageUploadUrLs UploadUrls(PackageCoordinates c, string version)
    {
        var basePath = $"/api/registry/packages/{c.Source}/{c.Publisher}/{c.Name}/versions/{version}/upload";
        return new PackageUploadUrLs
        {
            Index = $"{basePath}/index",
            Schema = $"{basePath}/schema",
            InstallationConfiguration = $"{basePath}/installation",
        };
    }
}
