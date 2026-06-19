#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Esc;

/// <summary>
/// An ESC secret rotator — the integration behind <c>fn::rotate::&lt;name&gt;</c> that cycles a secret in its
/// source system (e.g. an AWS IAM access key) and returns the new secret material. Like <see cref="IEscProvider"/>,
/// implementations are thin wrappers over one SDK and are exercised during a rotation action (not on open).
/// </summary>
public interface IEscRotator
{
    /// <summary>The rotator name as used in <c>fn::rotate::&lt;name&gt;</c> (e.g. <c>aws-iam</c>).</summary>
    string Name { get; }

    string Description { get; }

    EscSchemaSchema Inputs { get; }

    EscSchemaSchema Outputs { get; }

    /// <summary>
    /// Rotates the secret and returns the new value tree (secret leaves wrapped as <c>{ "fn::secret": ... }</c>).
    /// <paramref name="currentState"/> is the existing <c>state.current</c> (null on first rotation), used to
    /// retire the previous secret.
    /// </summary>
    Task<object?> RotateAsync(
        IReadOnlyDictionary<string, object?> inputs,
        IReadOnlyDictionary<string, object?>? currentState,
        CancellationToken ct);
}

/// <summary>Lookup over the registered <see cref="IEscRotator"/>s (one per <c>fn::rotate</c> integration).</summary>
public interface IEscRotatorRegistry
{
    IReadOnlyList<IEscRotator> All { get; }
    bool TryGet(string name, out IEscRotator rotator);
}
