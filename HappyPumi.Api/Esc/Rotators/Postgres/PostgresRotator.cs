#nullable enable

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Esc.Rotators.Postgres;

/// <summary>
/// The <c>fn::rotate::postgres</c> rotator: rotates a PostgreSQL role's password. On rotation it generates a
/// new password and applies it with the managing credentials, returning the new connection material.
/// Inputs (already interpolated):
/// <code>
/// host: db.example.com
/// port: 5432              # optional, defaults to 5432
/// database: app
/// managingUser: admin
/// managingPassword: { fn::secret: ... }
/// username: app_user      # the role to rotate
/// </code>
/// Output (the new <c>state.current</c>): <c>{ username, password: {fn::secret}, host, port, database }</c>.
/// </summary>
public sealed class PostgresRotator(IPostgresRotatorClient client) : IEscRotator
{
    private static readonly Regex SafeRole = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public string Name => "postgres";

    public string Description => "Rotates a PostgreSQL role's password via fn::rotate::postgres.";

    public EscSchemaSchema Inputs => new()
    {
        Type = "object",
        Required = new List<string> { "host", "database", "managingUser", "username" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["host"] = new() { Type = "string" },
            ["port"] = new() { Type = "integer", Description = "Defaults to 5432." },
            ["database"] = new() { Type = "string" },
            ["managingUser"] = new() { Type = "string", Description = "Role with privileges to ALTER the target role." },
            ["managingPassword"] = new() { Type = "string", Secret = true },
            ["username"] = new() { Type = "string", Description = "The role whose password is rotated." },
        },
    };

    public EscSchemaSchema Outputs => new()
    {
        Type = "object",
        Description = "The rotated connection material.",
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["username"] = new() { Type = "string" },
            ["password"] = new() { Type = "string", Secret = true },
        },
    };

    public async Task<object?> RotateAsync(
        IReadOnlyDictionary<string, object?> inputs,
        IReadOnlyDictionary<string, object?>? currentState,
        CancellationToken ct)
    {
        var host = EscProviderInputs.Require<string>(inputs, Name, "host");
        var database = EscProviderInputs.Require<string>(inputs, Name, "database");
        var managingUser = EscProviderInputs.Require<string>(inputs, Name, "managingUser");
        var username = EscProviderInputs.Require<string>(inputs, Name, "username");
        if (!SafeRole.IsMatch(username))
            throw new ArgumentException($"postgres 'username' must match {SafeRole}; got '{username}'.");
        var port = inputs.GetValueOrDefault("port") is { } p && int.TryParse(p.ToString(), out var parsed) ? parsed : 5432;

        var newPassword = GeneratePassword();
        var target = new PostgresRotationTarget(host, port, database, managingUser,
            inputs.GetValueOrDefault("managingPassword") as string, username);
        await client.SetPasswordAsync(target, newPassword, ct);

        return new Dictionary<string, object?>
        {
            ["username"] = username,
            ["password"] = EscProviderInputs.Secret(newPassword),
            ["host"] = host,
            ["port"] = (long)port,
            ["database"] = database,
        };
    }

    private static string GeneratePassword() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
}
