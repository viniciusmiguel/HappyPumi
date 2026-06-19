#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace HappyPumi.Api.Esc.Rotators.Postgres;

/// <summary>
/// Real <see cref="IPostgresRotatorClient"/>: connects with the managing credentials and runs
/// <c>ALTER ROLE … WITH PASSWORD</c>. The role identifier and password literal are escaped by doubling
/// quotes (DDL cannot be parameterized); the caller validates the role name shape.
/// </summary>
public sealed class PostgresRotatorClient : IPostgresRotatorClient
{
    public async Task SetPasswordAsync(PostgresRotationTarget target, string newPassword, CancellationToken ct)
    {
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = target.Host,
            Port = target.Port,
            Database = target.Database,
            Username = target.ManagingUser,
            Password = target.ManagingPassword,
        }.ConnectionString;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        var role = target.Username.Replace("\"", "\"\"");
        var password = newPassword.Replace("'", "''");
        command.CommandText = $"ALTER ROLE \"{role}\" WITH PASSWORD '{password}'";
        await command.ExecuteNonQueryAsync(ct);
    }
}
