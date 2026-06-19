#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Data.Entities;

/// <summary>A policy group. Key: (Org, Name). Stacks and applied-pack names are jsonb.</summary>
public sealed class PolicyGroupRow
{
    public string Org { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsOrgDefault { get; set; }
    public List<string> Stacks { get; set; } = new();
    public List<string> AppliedPolicyPacks { get; set; } = new();
}

/// <summary>One version of a policy pack. Key: (Org, Name, Version). DisplayName is denormalized per version.</summary>
public sealed class PolicyPackVersionRow
{
    public string Org { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long Version { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? VersionTag { get; set; }
    public bool Published { get; set; }
    public List<AppPolicy>? Policies { get; set; }
}

/// <summary>A policy violation observed during an update (the console's "Policy findings"). Key: Id
/// (sequence). The full violation payload is jsonb.</summary>
public sealed class PolicyFindingRow
{
    public long Id { get; set; }
    public string Org { get; set; } = default!;
    public PolicyViolationV2 Finding { get; set; } = default!;
}
