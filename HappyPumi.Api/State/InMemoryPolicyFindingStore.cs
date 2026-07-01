#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IPolicyFindingStore"/> (ADR-0005), keyed by org. Used by unit tests.</summary>
public sealed class InMemoryPolicyFindingStore : IPolicyFindingStore
{
    private readonly ConcurrentDictionary<string, List<PolicyViolationV2>> _findings = new();

    public void Record(string org, PolicyViolationV2 finding)
    {
        var list = _findings.GetOrAdd(org, _ => new List<PolicyViolationV2>());
        lock (list)
            list.Insert(0, finding); // newest first
    }

    public IReadOnlyList<PolicyViolationV2> List(string org)
    {
        if (!_findings.TryGetValue(org, out var list))
            return new List<PolicyViolationV2>();
        lock (list)
            return list.ToList();
    }
}
