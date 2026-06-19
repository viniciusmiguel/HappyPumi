using System.Collections.Generic;
using HappyPumi.Api.Esc.Providers.PulumiStacks;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IStackOutputsSource"/>: serves outputs from an in-memory map.</summary>
public sealed class FakeStackOutputsSource : IStackOutputsSource
{
    private readonly Dictionary<StackCoordinates, IReadOnlyDictionary<string, object?>> _outputs = new();

    public FakeStackOutputsSource With(StackCoordinates coordinates, Dictionary<string, object?> outputs)
    {
        _outputs[coordinates] = outputs;
        return this;
    }

    public IReadOnlyDictionary<string, object?>? Outputs(StackCoordinates coordinates)
        => _outputs.GetValueOrDefault(coordinates);
}
