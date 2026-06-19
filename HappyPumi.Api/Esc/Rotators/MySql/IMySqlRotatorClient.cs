#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Rotators.MySql;

/// <summary>Where to rotate: the managing connection plus the user whose password is being changed.</summary>
public readonly record struct MySqlRotationTarget(
    string Host, int Port, string Database, string ManagingUser, string? ManagingPassword, string Username, string UserHost);

/// <summary>
/// Owned, thin seam over MySQL (CLAUDE.md: wrap third-party I/O), used by the mysql rotator to set a user's
/// password. Faked in tests instead of connecting to a real database.
/// </summary>
public interface IMySqlRotatorClient
{
    Task SetPasswordAsync(MySqlRotationTarget target, string newPassword, CancellationToken ct);
}
