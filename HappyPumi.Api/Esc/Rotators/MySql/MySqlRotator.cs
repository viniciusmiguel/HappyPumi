#nullable enable

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Esc.Rotators.MySql;

/// <summary>
/// The <c>fn::rotate::mysql</c> rotator: rotates a MySQL user's password. On rotation it generates a new
/// password and applies it with the managing credentials, returning the new connection material.
/// Inputs (already interpolated):
/// <code>
/// host: db.example.com
/// port: 3306              # optional, defaults to 3306
/// database: app
/// managingUser: admin
/// managingPassword: { fn::secret: ... }
/// username: app_user      # the user to rotate
/// userHost: "%"           # optional, the user's host part, defaults to "%"
/// </code>
/// Output (the new <c>state.current</c>): <c>{ username, password: {fn::secret}, host, port, database }</c>.
/// </summary>
public sealed class MySqlRotator(IMySqlRotatorClient client) : IEscRotator
{
    private static readonly Regex SafeUser = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public string Name => "mysql";

    public string Description => "Rotates a MySQL user's password via fn::rotate::mysql.";

    public EscSchemaSchema Inputs => new()
    {
        Type = "object",
        Required = new List<string> { "host", "database", "managingUser", "username" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["host"] = new() { Type = "string" },
            ["port"] = new() { Type = "integer", Description = "Defaults to 3306." },
            ["database"] = new() { Type = "string" },
            ["managingUser"] = new() { Type = "string", Description = "User with privileges to ALTER the target user." },
            ["managingPassword"] = new() { Type = "string", Secret = true },
            ["username"] = new() { Type = "string", Description = "The user whose password is rotated." },
            ["userHost"] = new() { Type = "string", Description = "The user's host part (defaults to '%')." },
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
        if (!SafeUser.IsMatch(username))
            throw new ArgumentException($"mysql 'username' must match {SafeUser}; got '{username}'.");
        var port = inputs.GetValueOrDefault("port") is { } p && int.TryParse(p.ToString(), out var parsed) ? parsed : 3306;
        var userHost = inputs.GetValueOrDefault("userHost") as string ?? "%";

        var newPassword = GeneratePassword();
        var target = new MySqlRotationTarget(host, port, database, managingUser,
            inputs.GetValueOrDefault("managingPassword") as string, username, userHost);
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
