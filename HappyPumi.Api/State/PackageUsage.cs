#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>
/// Computes per-package usage stats for the registry's Private Components view: how many stacks deploy a
/// resource provided by a package. A package "name" owns resource types of the form "name:module:Type"
/// (e.g. the "widgets" package owns "widgets:index:Widget"), so a stack uses the package when any resource
/// in its latest checkpoint has that type prefix.
/// </summary>
public static class PackageUsage
{
    /// <summary>Number of stacks whose latest checkpoint contains a resource provided by <paramref name="packageName"/>.</summary>
    public static int StacksUsing(IReadOnlyCollection<StoredStack> stacks, string packageName)
    {
        var prefix = packageName + ":";
        return stacks.Count(s => StackResources.Extract(s.Deployment)
            .Any(r => r.Type.StartsWith(prefix, StringComparison.Ordinal)));
    }
}
