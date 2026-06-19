#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Esc;

/// <summary>
/// An ESC dynamic-value provider — the integration behind <c>fn::open::&lt;name&gt;</c> that fetches
/// credentials/config/secrets from an external source (e.g. a cloud key vault) at <em>open</em> time.
/// Implementations are thin wrappers over a single third-party SDK (CLAUDE.md: wrap libs behind an owned
/// interface). The returned tree is spliced into the environment's <c>values</c>; secret leaves are wrapped
/// as <c>{ "fn::secret": &lt;value&gt; }</c> so the evaluator flags them.
/// </summary>
public interface IEscProvider
{
    /// <summary>The provider name as used in <c>fn::open::&lt;name&gt;</c> (e.g. <c>azure-keyvault</c>).</summary>
    string Name { get; }

    /// <summary>Human-readable description (served by GetProviderSchema).</summary>
    string Description { get; }

    /// <summary>JSON-Schema for the provider's inputs (the map under <c>fn::open::&lt;name&gt;</c>).</summary>
    EscSchemaSchema Inputs { get; }

    /// <summary>JSON-Schema for the provider's outputs (the resolved value tree).</summary>
    EscSchemaSchema Outputs { get; }

    /// <summary>
    /// Resolves the provider's value tree from its already-interpolated <paramref name="inputs"/> (the map
    /// under <c>fn::open::&lt;name&gt;</c>). Throws <see cref="System.ArgumentException"/> with the offending
    /// value on malformed inputs.
    /// </summary>
    Task<object?> OpenAsync(IReadOnlyDictionary<string, object?> inputs, CancellationToken ct);
}

/// <summary>Lookup over the registered <see cref="IEscProvider"/>s (one per <c>fn::open</c> integration).</summary>
public interface IEscProviderRegistry
{
    IReadOnlyList<IEscProvider> All { get; }
    bool TryGet(string name, out IEscProvider provider);
}
