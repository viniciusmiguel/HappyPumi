using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Rotators.Postgres;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the fn::rotate::postgres rotator (against a fake DB client).</summary>
public sealed class PostgresRotatorTests
{
    private static Dictionary<string, object?> Inputs(string username = "app_user") => new()
    {
        ["host"] = "db.example.com",
        ["port"] = 5432L,
        ["database"] = "app",
        ["managingUser"] = "admin",
        ["managingPassword"] = "admin-pw",
        ["username"] = username,
    };

    [Fact]
    public async Task RotatesAndReturnsNewSecretPassword()
    {
        var client = new FakePostgresRotatorClient();
        var rotator = new PostgresRotator(client);

        var output = (Dictionary<string, object?>)(await rotator.RotateAsync(Inputs(), null, CancellationToken.None))!;

        Assert.Equal("app_user", output["username"]);
        var password = (Dictionary<string, object?>)output["password"]!;
        var rotated = (string)password["fn::secret"]!;
        Assert.NotEmpty(rotated);

        var call = client.Calls.Single();
        Assert.Equal("app_user", call.Target.Username);
        Assert.Equal("db.example.com", call.Target.Host);
        Assert.Equal(rotated, call.NewPassword); // the password applied is the one returned
    }

    [Fact]
    public async Task MissingHostThrows()
    {
        var rotator = new PostgresRotator(new FakePostgresRotatorClient());
        var inputs = Inputs();
        inputs.Remove("host");
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => rotator.RotateAsync(inputs, null, CancellationToken.None));
        Assert.Contains("host", ex.Message);
    }

    [Fact]
    public async Task UnsafeUsernameIsRejected()
    {
        var rotator = new PostgresRotator(new FakePostgresRotatorClient());
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => rotator.RotateAsync(Inputs("app\"; DROP ROLE x;--"), null, CancellationToken.None));
        Assert.Contains("username", ex.Message);
    }
}
