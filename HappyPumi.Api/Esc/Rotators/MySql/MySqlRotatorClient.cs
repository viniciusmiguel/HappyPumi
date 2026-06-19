#nullable enable

using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace HappyPumi.Api.Esc.Rotators.MySql;

/// <summary>
/// Real <see cref="IMySqlRotatorClient"/>: connects with the managing credentials and runs
/// <c>ALTER USER … IDENTIFIED BY</c>. The user/host identifiers and password are passed as command
/// parameters where possible; the user name is validated by the caller before reaching the statement.
/// </summary>
public sealed class MySqlRotatorClient : IMySqlRotatorClient
{
    public async Task SetPasswordAsync(MySqlRotationTarget target, string newPassword, CancellationToken ct)
    {
        var connectionString = new MySqlConnectionStringBuilder
        {
            Server = target.Host,
            Port = (uint)target.Port,
            Database = target.Database,
            UserID = target.ManagingUser,
            Password = target.ManagingPassword,
        }.ConnectionString;

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        var user = target.Username.Replace("`", "``");
        var host = target.UserHost.Replace("'", "''");
        var password = newPassword.Replace("'", "''");
        command.CommandText = $"ALTER USER `{user}`@'{host}' IDENTIFIED BY '{password}'";
        await command.ExecuteNonQueryAsync(ct);
    }
}
