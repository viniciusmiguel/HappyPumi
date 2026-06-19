using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>In-memory <see cref="IEscRotationHistory"/> for unit tests (the real impl is Postgres-backed).</summary>
public sealed class FakeEscRotationHistory : IEscRotationHistory
{
    private readonly Dictionary<EnvCoordinates, List<SecretRotationEvent>> _events = new();

    public void Record(EnvCoordinates coordinates, SecretRotationEvent rotationEvent)
        => (_events.TryGetValue(coordinates, out var list) ? list : _events[coordinates] = new()).Insert(0, rotationEvent);

    public IReadOnlyList<SecretRotationEvent> List(EnvCoordinates coordinates)
        => _events.TryGetValue(coordinates, out var list) ? list.ToArray() : new SecretRotationEvent[0];
}
