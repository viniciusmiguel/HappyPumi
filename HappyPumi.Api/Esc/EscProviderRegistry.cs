#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.Esc;

/// <summary>Registry over the DI-injected <see cref="IEscProvider"/>s, keyed by <see cref="IEscProvider.Name"/>.</summary>
public sealed class EscProviderRegistry : IEscProviderRegistry
{
    private readonly Dictionary<string, IEscProvider> _byName;

    public EscProviderRegistry(IEnumerable<IEscProvider> providers)
    {
        _byName = providers.ToDictionary(p => p.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<IEscProvider> All => _byName.Values.OrderBy(p => p.Name).ToList();

    public bool TryGet(string name, out IEscProvider provider) => _byName.TryGetValue(name, out provider!);
}
