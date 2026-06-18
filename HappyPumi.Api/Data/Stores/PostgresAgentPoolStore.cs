#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;
using Microsoft.EntityFrameworkCore;

namespace HappyPumi.Api.Data.Stores;

/// <summary>PostgreSQL-backed <see cref="IAgentPoolStore"/> (ADR-0005).</summary>
public sealed class PostgresAgentPoolStore(HappyPumiDbContext db) : IAgentPoolStore
{
    public AgentPoolRow CreatePool(string org, string name, string description)
    {
        var row = new AgentPoolRow
        {
            Id = Guid.NewGuid().ToString(),
            Org = org,
            Name = name,
            Description = description,
            // The agent presents this verbatim; "pul-" mirrors Pulumi's token shape (opaque to us).
            Token = "pul-" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            Created = DateTime.UtcNow,
        };
        db.AgentPools.Add(row);
        db.SaveChanges();
        return row;
    }

    public IReadOnlyList<AgentPoolRow> ListPools(string org)
        => db.AgentPools.AsNoTracking().Where(p => p.Org == org).OrderBy(p => p.Created).ToList();

    public AgentPoolRow? GetPool(string org, string poolId)
        => db.AgentPools.AsNoTracking().FirstOrDefault(p => p.Org == org && p.Id == poolId);

    public AgentPoolRow? FindByToken(string token)
        => db.AgentPools.AsNoTracking().FirstOrDefault(p => p.Token == token);
}
