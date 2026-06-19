#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence seam for CrossGuard policy (ENDPOINTS.md 5): policy groups and versioned policy packs,
/// per org. In-memory by default (ADR-0005). Safe for concurrent use.
/// </summary>
public interface IPolicyStore
{
    // Policy groups ---------------------------------------------------------
    IReadOnlyCollection<StoredPolicyGroup> ListGroups(string org);
    StoredPolicyGroup? GetGroup(string org, string name);

    /// <summary>Creates a group. Returns null when one of that name already exists.</summary>
    StoredPolicyGroup? NewGroup(string org, string name);

    /// <summary>Renames a group. Returns false when the source is missing or the target name is taken.</summary>
    bool RenameGroup(string org, string name, string newName);

    bool DeleteGroup(string org, string name);

    /// <summary>Adds a published pack to a group's applied set (creating the group if needed), as
    /// <c>pulumi policy enable</c> does. Idempotent. The default group is "default-policy-group".</summary>
    bool AddPackToGroup(string org, string group, string packName);

    /// <summary>Removes a pack from a group's applied set (<c>pulumi policy disable</c>). False if absent.</summary>
    bool RemovePackFromGroup(string org, string group, string packName);

    // Policy packs ----------------------------------------------------------
    IReadOnlyCollection<StoredPolicyPack> ListPacks(string org);

    /// <summary>Creates the next version of a pack (auto-incrementing), returning the new version number.
    /// <paramref name="versionTag"/> is the CLI's semver tag (e.g. "1.0.0"), used to complete the publish.</summary>
    long CreatePackVersion(string org, string name, string displayName, List<AppPolicy>? policies, string? versionTag = null);

    StoredPolicyPack? GetPack(string org, string name);
    bool CompletePack(string org, string name, long version);

    /// <summary>Completes a pack version where the segment may be a numeric version OR the CLI's semver tag
    /// (e.g. "1.0.0"); the tag resolves to its version, falling back to the newest. False when unknown.</summary>
    bool CompletePackVersion(string org, string name, string versionOrTag)
    {
        var pack = GetPack(org, name);
        if (pack is null)
            return false;
        long? version = long.TryParse(versionOrTag, out var numeric)
            ? numeric
            : pack.Versions.Values.FirstOrDefault(x => x.VersionTag == versionOrTag)?.Version
              ?? (pack.Versions.Count == 0 ? null : pack.Versions.Keys.Max());
        return version is not null && CompletePack(org, name, version.Value);
    }
    bool DeletePackVersion(string org, string name, long version);
    bool DeletePack(string org, string name);
}
