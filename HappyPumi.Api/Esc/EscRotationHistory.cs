#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>
/// Persistence seam for an environment's secret-rotation events (newest first). Backed by PostgreSQL
/// (see <c>PostgresEscRotationHistory</c>).
/// </summary>
public interface IEscRotationHistory
{
    void Record(EnvCoordinates coordinates, SecretRotationEvent rotationEvent);
    IReadOnlyList<SecretRotationEvent> List(EnvCoordinates coordinates);
}
