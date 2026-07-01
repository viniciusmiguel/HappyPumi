#nullable enable

using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Resolves a project-template selector (a template <c>name</c>/<c>url</c> query value) to the latest matching
/// <see cref="StoredTemplateVersion"/> in the registry (templates PR1). Resolution is deterministic so
/// component tests can seed a version then GET it. A <c>source/publisher/name</c> selector is matched on its
/// trailing name segment; otherwise the whole value is treated as a name filter.
/// </summary>
internal static class ProjectTemplateResolver
{
    /// <summary>The latest registry template matching <paramref name="selector"/>, or null when none/blank.</summary>
    public static StoredTemplateVersion? Resolve(ITemplateRegistry registry, string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return null;
        var name = selector.Split('/').Last();
        return registry.ListLatest(name).FirstOrDefault(t => t.Coordinates.Name == name)
            ?? registry.ListLatest(name).FirstOrDefault();
    }
}
