using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Rotators.MySql;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IMySqlRotatorClient"/>: records the ALTER USER it was asked to run.</summary>
public sealed class FakeMySqlRotatorClient : IMySqlRotatorClient
{
    public List<(MySqlRotationTarget Target, string NewPassword)> Calls { get; } = new();

    public Task SetPasswordAsync(MySqlRotationTarget target, string newPassword, CancellationToken ct)
    {
        Calls.Add((target, newPassword));
        return Task.CompletedTask;
    }
}
