using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Rotators.Postgres;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IPostgresRotatorClient"/>: records the ALTER ROLE it was asked to run.</summary>
public sealed class FakePostgresRotatorClient : IPostgresRotatorClient
{
    public List<(PostgresRotationTarget Target, string NewPassword)> Calls { get; } = new();

    public Task SetPasswordAsync(PostgresRotationTarget target, string newPassword, CancellationToken ct)
    {
        Calls.Add((target, newPassword));
        return Task.CompletedTask;
    }
}
