#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Resolves the <c>imports</c> top-level key by recursively loading the referenced environments and
/// deep-merging their <c>values</c>. Precedence follows ESC: imports are applied in list order (a later
/// import overrides an earlier one), and the importing environment's own <c>values</c> override all of
/// its imports. Cycles are broken by tracking the environments currently being resolved.
/// </summary>
public static class EnvironmentImporter
{
    /// <summary>
    /// The merged <c>values</c> tree for <paramref name="coords"/>, following imports depth-first.
    /// <paramref name="resolveRoot"/> returns the parsed root of an imported environment, or null if missing.
    /// </summary>
    public static Dictionary<string, object?> MergedValues(
        EnvCoordinates coords,
        Dictionary<string, object?> root,
        Func<EnvCoordinates, Dictionary<string, object?>?> resolveRoot,
        ISet<EnvCoordinates>? visiting = null)
    {
        visiting ??= new HashSet<EnvCoordinates>();
        var merged = new Dictionary<string, object?>();
        if (!visiting.Add(coords))
            return merged; // import cycle — stop here rather than recurse forever

        foreach (var importRef in ImportsOf(root))
        {
            var imported = ParseImport(importRef, coords);
            var importedRoot = resolveRoot(imported);
            if (importedRoot is not null)
                DeepMerge(merged, MergedValues(imported, importedRoot, resolveRoot, visiting));
        }

        DeepMerge(merged, EnvironmentEvaluator.ValuesOf(root));
        visiting.Remove(coords);
        return merged;
    }

    private static IEnumerable<string> ImportsOf(Dictionary<string, object?> root) =>
        root.TryGetValue("imports", out var v) && v is List<object?> list
            ? list.OfType<string>()
            : Enumerable.Empty<string>();

    // An import ref is "project/name" or bare "name" (default project = the importer's), optionally
    // pinned with "@version"; the version pin is ignored for now (the current revision is always used).
    private static EnvCoordinates ParseImport(string importRef, EnvCoordinates importer)
    {
        var withoutVersion = importRef.Split('@', 2)[0];
        var parts = withoutVersion.Split('/', 2);
        return parts.Length == 2
            ? new EnvCoordinates(importer.Org, parts[0], parts[1])
            : new EnvCoordinates(importer.Org, importer.Project, parts[0]);
    }

    // Recursively merge source into target; nested maps merge, everything else replaces (deep-cloned).
    private static void DeepMerge(Dictionary<string, object?> target, Dictionary<string, object?> source)
    {
        foreach (var (key, value) in source)
        {
            if (target.TryGetValue(key, out var existing)
                && existing is Dictionary<string, object?> a
                && value is Dictionary<string, object?> b)
                DeepMerge(a, b);
            else
                target[key] = Clone(value);
        }
    }

    private static object? Clone(object? node) => node switch
    {
        Dictionary<string, object?> map => map.ToDictionary(kv => kv.Key, kv => Clone(kv.Value)),
        List<object?> list => list.Select(Clone).ToList(),
        _ => node,
    };
}
