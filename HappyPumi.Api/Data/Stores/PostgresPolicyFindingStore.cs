#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IPolicyFindingStore"/> (ADR-0005).</summary>
public sealed class PostgresPolicyFindingStore(HappyPumiDbContext db) : IPolicyFindingStore
{
    public void Record(string org, PolicyViolationV2 finding)
    {
        db.PolicyFindings.Add(new PolicyFindingRow { Org = org, Finding = finding });
        db.SaveChanges();
    }

    public IReadOnlyList<PolicyViolationV2> List(string org)
        => db.PolicyFindings.AsNoTracking()
            .Where(f => f.Org == org)
            .OrderByDescending(f => f.Id)
            .Select(f => f.Finding)
            .ToList();
}
