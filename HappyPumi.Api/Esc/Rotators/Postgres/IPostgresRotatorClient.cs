#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Rotators.Postgres;

/// <summary>Where to rotate: the managing connection plus the role whose password is being changed.</summary>
public readonly record struct PostgresRotationTarget(
    string Host, int Port, string Database, string ManagingUser, string? ManagingPassword, string Username);

/// <summary>
/// Owned, thin seam over PostgreSQL (CLAUDE.md: wrap third-party I/O), used by the postgres rotator to set a
/// role's password. Faked in tests instead of connecting to a real database.
/// </summary>
public interface IPostgresRotatorClient
{
    Task SetPasswordAsync(PostgresRotationTarget target, string newPassword, CancellationToken ct);
}
