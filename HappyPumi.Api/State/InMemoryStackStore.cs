#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

    public IReadOnlyCollection<StoredStack> All() => _stacks.Values.ToArray();

    public bool RecordHistory(StackCoordinates coordinates, StoredHistoryEntry entry)
    {
        if (!_stacks.TryGetValue(coordinates, out var stack))
            return false;
        stack.History.Add(entry);
        return true;
    }

    public StoredStack? SetTag(StackCoordinates coordinates, string name, string value)
    {
        if (!_stacks.TryGetValue(coordinates, out var stack))
            return null;
        stack.Tags[name] = value;
        return stack;
    }

    public StoredStack? ReplaceTags(StackCoordinates coordinates, IReadOnlyDictionary<string, string> tags)
    {
        if (!_stacks.TryGetValue(coordinates, out var stack))
            return null;
        stack.Tags.Clear();
        foreach (var (name, value) in tags)
            stack.Tags[name] = value;
        return stack;
    }

    public StoredStack? Rename(StackCoordinates from, StackCoordinates to, out bool collision)
    {
        collision = false;
        if (!_stacks.TryGetValue(from, out var existing))
            return null;
        if (from == to)
            return existing;
        if (_stacks.ContainsKey(to))
        {
            collision = true;
            return null;
        }

        // Re-key under the new coordinates, preserving state/config/history/tags.
        var moved = new StoredStack
        {
            Coordinates = to,
            Version = existing.Version,
            Config = existing.Config,
            Deployment = existing.Deployment,
        };
        foreach (var (k, v) in existing.Tags)
            moved.Tags[k] = v;
        moved.History.AddRange(existing.History);
        if (!_stacks.TryAdd(to, moved))
        {
            collision = true;
            return null;
        }
        _stacks.TryRemove(from, out _);
        return moved;
    }
}
