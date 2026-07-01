#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>
/// PostgreSQL-backed <see cref="IAuthPolicyStore"/> (policy-results PR2, ADR-0005): auth policies keyed by
/// (Org, PolicyId). The rule set round-trips through a jsonb column. Mirrors PostgresChangeGateStore.
/// </summary>
public sealed class PostgresAuthPolicyStore(HappyPumiDbContext db) : IAuthPolicyStore
{
    public StoredAuthPolicy? Get(string org, string policyId)
    {
        var row = db.AuthPolicies.AsNoTracking().FirstOrDefault(p => p.Org == org && p.PolicyId == policyId);
        return row is null ? null : Map(row);
    }

    public StoredAuthPolicy Upsert(string org, string policyId, List<AuthPolicyDefinition> policies)
    {
        var row = db.AuthPolicies.FirstOrDefault(p => p.Org == org && p.PolicyId == policyId);
        if (row is null)
        {
            row = new AuthPolicyRow { Org = org, PolicyId = policyId };
            db.AuthPolicies.Add(row);
        }
        else
        {
            row.Version += 1; // an existing policy's rule set is being replaced — bump the version
        }
        row.Policies = policies.ToList();
        row.Modified = DateTime.UtcNow;
        db.SaveChanges();
        return Map(row);
    }

    private static StoredAuthPolicy Map(AuthPolicyRow r) => new()
    {
        Org = r.Org, PolicyId = r.PolicyId, Policies = r.Policies.ToList(),
        Version = r.Version, Created = r.Created, Modified = r.Modified,
    };
}
