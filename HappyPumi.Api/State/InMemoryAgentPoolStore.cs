#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Data.Entities;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IAgentPoolStore"/> (ADR-0005), keyed by pool id. Used by unit tests.</summary>
public sealed class InMemoryAgentPoolStore : IAgentPoolStore
{
    private readonly ConcurrentDictionary<string, AgentPoolRow> _byId = new();

    public AgentPoolRow CreatePool(string org, string name, string description)
    {
        var row = new AgentPoolRow
        {
            Id = Guid.NewGuid().ToString(),
            Org = org,
            Name = name,
            Description = description,
            Token = "pul-" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            Created = DateTime.UtcNow,
        };
        _byId[row.Id] = row;
        return row;
    }

    public IReadOnlyList<AgentPoolRow> ListPools(string org)
        => _byId.Values.Where(p => p.Org == org).OrderBy(p => p.Created).ToList();

    public AgentPoolRow? GetPool(string org, string poolId)
        => _byId.TryGetValue(poolId, out var row) && row.Org == org ? row : null;

    public AgentPoolRow? FindByToken(string token)
        => _byId.Values.FirstOrDefault(p => p.Token == token);

    public bool DeletePool(string org, string poolId)
        => GetPool(org, poolId) is not null && _byId.TryRemove(poolId, out _);

    public AgentPoolRow? UpdatePool(string org, string poolId, string? name, string? description)
    {
        var row = GetPool(org, poolId);
        if (row is null) return null;
        if (name is not null) row.Name = name;
        if (description is not null) row.Description = description;
        return row;
    }
}
