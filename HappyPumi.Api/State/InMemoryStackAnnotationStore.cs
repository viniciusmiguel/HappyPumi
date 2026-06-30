#nullable enable

using System.Collections.Concurrent;

namespace HappyPumi.Api.State;

/// <summary>
/// In-memory <see cref="IStackAnnotationStore"/> backed by a concurrent dictionary (ADR-0005). Default
/// store for local dev and tests; state is lost on restart. The PostgreSQL implementation replaces it
/// behind the interface without endpoint changes.
/// </summary>
public sealed class InMemoryStackAnnotationStore : IStackAnnotationStore
{
    // Keyed by (coordinates, kind) so each annotation kind holds one payload per stack.
    private readonly ConcurrentDictionary<(StackCoordinates, string), object> _annotations = new();

    public object? Get(StackCoordinates coordinates, string kind)
        => _annotations.TryGetValue((coordinates, kind), out var payload) ? payload : null;

    public void Set(StackCoordinates coordinates, string kind, object payload)
        => _annotations[(coordinates, kind)] = payload;
}
