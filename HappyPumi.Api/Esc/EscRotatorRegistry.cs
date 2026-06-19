#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.Esc;

/// <summary>Registry over the DI-injected <see cref="IEscRotator"/>s, keyed by <see cref="IEscRotator.Name"/>.</summary>
public sealed class EscRotatorRegistry : IEscRotatorRegistry
{
    private readonly Dictionary<string, IEscRotator> _byName;

    public EscRotatorRegistry(IEnumerable<IEscRotator> rotators)
    {
        _byName = rotators.ToDictionary(r => r.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<IEscRotator> All => _byName.Values.OrderBy(r => r.Name).ToList();

    public bool TryGet(string name, out IEscRotator rotator) => _byName.TryGetValue(name, out rotator!);
}
