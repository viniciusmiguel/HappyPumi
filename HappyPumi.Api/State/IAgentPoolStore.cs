#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Data.Entities;

namespace HappyPumi.Api.State;

/// <summary>
/// Persistence for workflow-runner (agent) pools and their access tokens. The token minted by
/// <see cref="CreatePool"/> is what the customer-managed workflow agent authenticates with; agent
/// endpoints validate it via <see cref="FindByToken"/> (ADR-0007 follow-up: real credential validation).
/// </summary>
public interface IAgentPoolStore
{
    /// <summary>Creates a pool under the org and mints its access token.</summary>
    AgentPoolRow CreatePool(string org, string name, string description);

    IReadOnlyList<AgentPoolRow> ListPools(string org);

    AgentPoolRow? GetPool(string org, string poolId);

    /// <summary>The pool a presented token belongs to, or null if the token is unknown.</summary>
    AgentPoolRow? FindByToken(string token);

    /// <summary>Deletes a pool by id within the org; false when no such pool exists.</summary>
    bool DeletePool(string org, string poolId);

    /// <summary>Patches the supplied (non-null) fields by id; null when the pool is missing.</summary>
    AgentPoolRow? UpdatePool(string org, string poolId, string? name, string? description);
}
