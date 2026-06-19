#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc.Providers.PulumiStacks;

/// <summary>
/// Reads another stack's outputs for the <c>fn::open::pulumi-stacks</c> provider. A focused seam over
/// <see cref="IStackStore"/> so the provider (a singleton) can resolve it inside a request scope, and so it
/// can be faked in tests without standing up the whole stack store.
/// </summary>
public interface IStackOutputsSource
{
    /// <summary>The stack's root outputs, or null when the stack does not exist.</summary>
    IReadOnlyDictionary<string, object?>? Outputs(StackCoordinates coordinates);
}

/// <summary>Default <see cref="IStackOutputsSource"/>: reads the latest checkpoint via <see cref="IStackStore"/>.</summary>
public sealed class StackOutputsSource(IStackStore stacks) : IStackOutputsSource
{
    public IReadOnlyDictionary<string, object?>? Outputs(StackCoordinates coordinates)
    {
        var stack = stacks.Find(coordinates);
        return stack is null ? null : StackOutputs.Extract(stack.Deployment);
    }
}
