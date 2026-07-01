#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A persisted auth policy (policy-results PR2, ADR-0005). Key: (Org, PolicyId). The <see cref="Policies"/>
/// rule set is stored as jsonb; scalar columns carry version + timestamps.
/// </summary>
public sealed class AuthPolicyRow
{
    public string Org { get; set; } = default!;
    public string PolicyId { get; set; } = default!;

    /// <summary>The auth-policy rule set (jsonb).</summary>
    public List<AuthPolicyDefinition> Policies { get; set; } = new();

    public long Version { get; set; } = 1;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}
