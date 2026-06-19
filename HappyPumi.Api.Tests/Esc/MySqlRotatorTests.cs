using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Rotators.MySql;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the fn::rotate::mysql rotator (against a fake DB client).</summary>
public sealed class MySqlRotatorTests
{
    private static Dictionary<string, object?> Inputs(string username = "app_user") => new()
    {
        ["host"] = "db.example.com",
        ["database"] = "app",
        ["managingUser"] = "admin",
        ["managingPassword"] = "admin-pw",
        ["username"] = username,
    };

    [Fact]
    public async Task RotatesAndReturnsNewSecretPassword()
    {
        var client = new FakeMySqlRotatorClient();
        var rotator = new MySqlRotator(client);

        var output = (Dictionary<string, object?>)(await rotator.RotateAsync(Inputs(), null, CancellationToken.None))!;

        Assert.Equal("app_user", output["username"]);
        var rotated = (string)((Dictionary<string, object?>)output["password"]!)["fn::secret"]!;
        Assert.NotEmpty(rotated);

        var call = client.Calls.Single();
        Assert.Equal("app_user", call.Target.Username);
        Assert.Equal("%", call.Target.UserHost); // default host part
        Assert.Equal(rotated, call.NewPassword);
    }

    [Fact]
    public async Task MissingDatabaseThrows()
    {
        var rotator = new MySqlRotator(new FakeMySqlRotatorClient());
        var inputs = Inputs();
        inputs.Remove("database");
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => rotator.RotateAsync(inputs, null, CancellationToken.None));
        Assert.Contains("database", ex.Message);
    }

    [Fact]
    public async Task UnsafeUsernameIsRejected()
    {
        var rotator = new MySqlRotator(new FakeMySqlRotatorClient());
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => rotator.RotateAsync(Inputs("app`; DROP USER x"), null, CancellationToken.None));
        Assert.Contains("username", ex.Message);
    }
}
