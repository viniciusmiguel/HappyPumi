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

    public static Template ToTemplate(StoredTemplateVersion tmpl) => new()
    {
        Source = tmpl.Coordinates.Source,
        Publisher = tmpl.Coordinates.Publisher,
        Name = tmpl.Coordinates.Name,
        DisplayName = tmpl.Coordinates.Name,
        Language = tmpl.Language ?? string.Empty,
        Description = tmpl.Description,
        Visibility = "public",
        UpdatedAt = tmpl.UpdatedAt,
        Url = $"/api/registry/templates/{tmpl.Coordinates.Source}/{tmpl.Coordinates.Publisher}/{tmpl.Coordinates.Name}",
        DownloadUrl = $"/api/registry/templates/{tmpl.Coordinates.Source}/{tmpl.Coordinates.Publisher}/{tmpl.Coordinates.Name}/versions/{tmpl.Version}/archive",
    };

    public static GetTemplateResponse ToTemplateResponse(StoredTemplateVersion tmpl)
    {
        var t = ToTemplate(tmpl);
        return new GetTemplateResponse
        {
            Source = t.Source, Publisher = t.Publisher, Name = t.Name, DisplayName = t.DisplayName,
            Language = t.Language, Description = t.Description, Visibility = t.Visibility,
            UpdatedAt = t.UpdatedAt, Url = t.Url, DownloadUrl = t.DownloadUrl,
        };
    }

    public static TemplateUploadUrLs TemplateUploadUrls(TemplateCoordinates c, string version) => new()
    {
        Archive = $"/api/registry/templates/{c.Source}/{c.Publisher}/{c.Name}/versions/{version}/upload/archive",
    };
}
