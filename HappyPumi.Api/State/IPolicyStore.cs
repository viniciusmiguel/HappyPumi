#nullable enable

using System.Collections.Generic;
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

    // Policy packs ----------------------------------------------------------
    IReadOnlyCollection<StoredPolicyPack> ListPacks(string org);

    /// <summary>Creates the next version of a pack (auto-incrementing), returning the new version number.</summary>
    long CreatePackVersion(string org, string name, string displayName, List<AppPolicy>? policies);

    StoredPolicyPack? GetPack(string org, string name);
    bool CompletePack(string org, string name, long version);
    bool DeletePackVersion(string org, string name, long version);
    bool DeletePack(string org, string name);
}
