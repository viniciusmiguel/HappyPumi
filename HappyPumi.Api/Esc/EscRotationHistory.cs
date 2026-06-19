#nullable enable

using System.Collections.Concurrent;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>Records and lists an environment's secret-rotation events (newest first).</summary>
public interface IEscRotationHistory
{
    void Record(EnvCoordinates coordinates, SecretRotationEvent rotationEvent);
    IReadOnlyList<SecretRotationEvent> List(EnvCoordinates coordinates);
}

/// <summary>
/// In-memory <see cref="IEscRotationHistory"/>. Rotation history is operational telemetry, not core state, so
/// it is kept in-process for now (a durable store is a follow-up, like the open-session store).
/// </summary>
public sealed class EscRotationHistory : IEscRotationHistory
{
    private readonly ConcurrentDictionary<EnvCoordinates, List<SecretRotationEvent>> _events = new();

    public void Record(EnvCoordinates coordinates, SecretRotationEvent rotationEvent)
    {
        var list = _events.GetOrAdd(coordinates, _ => new List<SecretRotationEvent>());
        lock (list)
            list.Insert(0, rotationEvent); // newest first
    }

    public IReadOnlyList<SecretRotationEvent> List(EnvCoordinates coordinates)
    {
        if (!_events.TryGetValue(coordinates, out var list))
            return new List<SecretRotationEvent>();
        lock (list)
            return list.ToArray();
    }
}
