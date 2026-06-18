#nullable enable

using System.Collections.Concurrent;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.State;

/// <summary>
/// In-memory <see cref="IStackStore"/> backed by a concurrent dictionary. This is the default store
/// for local dev and tests (ADR-0005); state is lost on restart. A durable PostgreSQL implementation
/// replaces it behind the interface without endpoint changes.
/// </summary>
public sealed class InMemoryStackStore : IStackStore
{
    private readonly ConcurrentDictionary<StackCoordinates, StoredStack> _stacks = new();

    public bool ProjectExists(string org, string project)
    {
        foreach (var coordinates in _stacks.Keys)
            if (coordinates.Org == org && coordinates.Project == project)
                return true;
        return false;
    }

    public StoredStack? Find(StackCoordinates coordinates)
        => _stacks.TryGetValue(coordinates, out var stack) ? stack : null;

    public bool TryCreate(StoredStack stack)
        => _stacks.TryAdd(stack.Coordinates, stack);

    public bool Delete(StackCoordinates coordinates)
        => _stacks.TryRemove(coordinates, out _);

    public StoredStack? SetConfig(StackCoordinates coordinates, AppStackConfig config)
    {
        if (!_stacks.TryGetValue(coordinates, out var stack))
            return null;
        stack.Config = config;
        return stack;
    }

    public bool ClearConfig(StackCoordinates coordinates)
    {
        if (!_stacks.TryGetValue(coordinates, out var stack))
            return false;
        stack.Config = null;
        return true;
    }

    public StoredStack? SetDeployment(StackCoordinates coordinates, AppUntypedDeployment deployment, bool bumpVersion)
    {
        if (!_stacks.TryGetValue(coordinates, out var stack))
            return null;
        stack.Deployment = deployment;
        if (bumpVersion)
            stack.Version++;
        return stack;
    }
}
