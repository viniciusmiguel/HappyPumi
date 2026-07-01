#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IAuthPolicyStore"/> (ADR-0005), keyed by (org, policyId). Used by unit tests.</summary>
public sealed class InMemoryAuthPolicyStore : IAuthPolicyStore
{
    private readonly ConcurrentDictionary<(string Org, string PolicyId), StoredAuthPolicy> _policies = new();

    public StoredAuthPolicy? Get(string org, string policyId)
        => _policies.TryGetValue((org, policyId), out var p) ? p : null;

    public StoredAuthPolicy Upsert(string org, string policyId, List<AuthPolicyDefinition> policies)
    {
        var existing = Get(org, policyId);
        var updated = new StoredAuthPolicy
        {
            Org = org,
            PolicyId = policyId,
            Policies = policies.ToList(),
            Version = (existing?.Version ?? 0) + 1,
            Created = existing?.Created ?? DateTime.UtcNow,
            Modified = DateTime.UtcNow,
        };
        _policies[(org, policyId)] = updated;
        return updated;
    }
}
