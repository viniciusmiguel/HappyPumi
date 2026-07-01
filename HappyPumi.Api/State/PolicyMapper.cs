#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>Maps CrossGuard policy domain records to their wire DTOs.</summary>
public static class PolicyMapper
{
    private const string StackEntity = "stack";
    private const string EnabledMode = "enabled";

    public static AppPolicyGroupSummary ToGroupSummary(StoredPolicyGroup group) => new()
    {
        Name = group.Name,
        IsOrgDefault = group.IsOrgDefault,
        EntityType = StackEntity,
        Mode = EnabledMode,
        NumStacks = group.Stacks.Count,
        NumEnabledPolicyPacks = group.AppliedPolicyPacks.Count,
        NumAccounts = 0,
    };

    public static PolicyGroup ToGroup(StoredPolicyGroup group) => new()
    {
        Name = group.Name,
        IsOrgDefault = group.IsOrgDefault,
        EntityType = StackEntity,
        Mode = EnabledMode,
        Accounts = new List<string>(),
        AppliedPolicyPacks = new List<AppPolicyPackMetadata>(),
        Stacks = new List<AppPulumiStackReference>(),
    };

    public static AppPolicyPackWithVersions ToPackWithVersions(StoredPolicyPack pack)
    {
        var versions = pack.Versions.Values.OrderBy(v => v.Version).ToList();
        return new AppPolicyPackWithVersions
        {
            Name = pack.Name,
            DisplayName = pack.DisplayName,
            Versions = versions.Select(v => v.Version).ToList(),
            VersionTags = versions.Select(v => v.VersionTag ?? v.Version.ToString()).ToList(),
        };
    }

    public static AppGetPolicyPackResponse ToPackResponse(StoredPolicyPack pack, StoredPolicyPackVersion version) => new()
    {
        Name = pack.Name,
        DisplayName = pack.DisplayName,
        Version = version.Version,
        VersionTag = version.VersionTag ?? version.Version.ToString(),
        Policies = version.Policies ?? new List<AppPolicy>(),
        Applied = false,
    };

    /// <summary>Maps a stored pack version to the registry-metadata response (GetOrgRegistryPolicyPack). The
    /// publisher is the owning org; only "private" packs are modelled (there is no public registry here).</summary>
    public static GetRegistryPolicyPackVersionResponse ToRegistryResponse(
        string org, StoredPolicyPack pack, StoredPolicyPackVersion version) => new()
    {
        Policies = version.Policies ?? new List<AppPolicy>(),
        PolicyPack = new RegistryPolicyPack
        {
            Id = pack.Name,
            Name = pack.Name,
            DisplayName = string.IsNullOrEmpty(pack.DisplayName) ? pack.Name : pack.DisplayName,
            Publisher = org,
            Source = "private",
            AccessLevel = "admin",
            Version = version.VersionTag ?? version.Version.ToString(),
            EnforcementLevels = new List<string>(),
        },
    };
}
