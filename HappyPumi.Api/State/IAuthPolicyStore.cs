#nullable enable

using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// A persisted authentication policy (policy-results PR2, ADR-0005), keyed by (Org, PolicyId). The policy id
/// is the OIDC-issuer registration id the rules apply to. <see cref="Policies"/> is the wire rule set, stored
/// as jsonb. Each update bumps <see cref="Version"/> and stamps <see cref="Modified"/>.
/// </summary>
public sealed class StoredAuthPolicy
{
    public required string Org { get; init; }
    public required string PolicyId { get; init; }
    public List<AuthPolicyDefinition> Policies { get; set; } = new();
    public long Version { get; set; } = 1;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persistence seam for auth policies (ADR-0005). Backed by PostgreSQL in production and an in-memory map in
/// unit tests. <see cref="Upsert"/> creates the policy or overwrites its rule set, bumping the version and
/// modified timestamp; the first write is version 1.
/// </summary>
public interface IAuthPolicyStore
{
    /// <summary>The stored policy for (org, policyId), or null when never set.</summary>
    StoredAuthPolicy? Get(string org, string policyId);

    /// <summary>Creates or replaces the policy's rule set; returns the persisted record.</summary>
    StoredAuthPolicy Upsert(string org, string policyId, List<AuthPolicyDefinition> policies);
}
