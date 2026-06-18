#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>A policy group: a named set of applied policy packs scoped to stacks (CrossGuard, ENDPOINTS.md 5).</summary>
public sealed class StoredPolicyGroup
{
    public required string Name { get; set; }
    public bool IsOrgDefault { get; set; }
    public List<string> Stacks { get; } = new();
    public List<string> AppliedPolicyPacks { get; } = new();
}

/// <summary>One published (or pending) version of a policy pack.</summary>
public sealed class StoredPolicyPackVersion
{
    public required long Version { get; init; }
    public string? VersionTag { get; set; }
    public bool Published { get; set; }
    public List<AppPolicy>? Policies { get; set; }
}

/// <summary>A policy pack and its versions, owned by an org.</summary>
public sealed class StoredPolicyPack
{
    public required string Name { get; init; }
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<long, StoredPolicyPackVersion> Versions { get; } = new();
}
