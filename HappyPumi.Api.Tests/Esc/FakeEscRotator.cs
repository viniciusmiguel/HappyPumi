using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) rotator: returns a canned new secret and records the state it was given.</summary>
public sealed class FakeEscRotator : IEscRotator
{
    public string Name => "test-rotator";
    public string Description => "Test rotator.";
    public EscSchemaSchema Inputs => new() { Type = "object" };
    public EscSchemaSchema Outputs => new() { Type = "object" };

    public IReadOnlyDictionary<string, object?>? ReceivedCurrent { get; private set; }

    public Task<object?> RotateAsync(
        IReadOnlyDictionary<string, object?> inputs,
        IReadOnlyDictionary<string, object?>? currentState,
        CancellationToken ct)
    {
        ReceivedCurrent = currentState;
        var result = new Dictionary<string, object?> { ["password"] = new Dictionary<string, object?> { ["fn::secret"] = "new-pw" } };
        return Task.FromResult<object?>(result);
    }
}
