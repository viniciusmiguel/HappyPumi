#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.State;

/// <summary>Persistence seam for the template registry (ENDPOINTS.md 4), in-memory by default (ADR-0005).</summary>
public interface ITemplateRegistry
{
    /// <summary>The latest version of each template, optionally filtered by name (substring, case-insensitive).</summary>
    IReadOnlyCollection<StoredTemplateVersion> ListLatest(string? nameFilter);

    /// <summary>All versions of one template, newest first.</summary>
    IReadOnlyList<StoredTemplateVersion> ListVersions(TemplateCoordinates coordinates);

    /// <summary>A specific version, or the most recent when <paramref name="version"/> is "latest".</summary>
    StoredTemplateVersion? Get(TemplateCoordinates coordinates, string version);

    StoredTemplateVersion StartPublish(TemplateCoordinates coordinates, string version);
    bool CompletePublish(TemplateCoordinates coordinates, string version);
    bool Delete(TemplateCoordinates coordinates, string version);
}
