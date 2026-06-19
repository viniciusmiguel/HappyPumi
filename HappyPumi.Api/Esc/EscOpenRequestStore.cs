#nullable enable

using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>A request to open a protected environment: the desired access + grant durations, as a change request.</summary>
public sealed record EscOpenRequest(string Id, long AccessDurationSeconds, long GrantExpirationSeconds, long BaseRevision);

/// <summary>
/// Persistence seam for open-access requests against protected environments (the approvals entry point).
/// Backed by PostgreSQL (see <c>PostgresEscOpenRequestStore</c>).
/// </summary>
public interface IEscOpenRequestStore
{
    EscOpenRequest Create(EnvCoordinates environment, long accessDurationSeconds, long grantExpirationSeconds, long baseRevision);
    EscOpenRequest? Get(EnvCoordinates environment, string changeRequestId);
    EscOpenRequest? Update(EnvCoordinates environment, string changeRequestId, long accessDurationSeconds, long grantExpirationSeconds);
}
