#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPumi.Api.Esc.Providers.PulumiStacks;

/// <summary>
/// The <c>fn::open::pulumi-stacks</c> provider: exposes other stacks' outputs for cross-stack references.
/// Inputs (already interpolated by the opener):
/// <code>
/// organization: happypumi
/// stacks:
///   api: { project: backend,  stack: prod }
///   web: { project: frontend, stack: prod }
/// </code>
/// Output is <c>{ api: {&lt;outputs&gt;}, web: {&lt;outputs&gt;} }</c>. A missing stack yields null.
/// </summary>
/// <remarks>
/// Reads from HappyPumi's own stack store, so it needs no cloud SDK. The store is request-scoped while
/// providers are singletons, so outputs are read inside a fresh scope created per open. Secret stack
/// outputs are currently surfaced as-is (decrypting them is a follow-up).
/// </remarks>
public sealed class PulumiStacksProvider(IServiceScopeFactory scopes) : IEscProvider
{
    public string Name => "pulumi-stacks";

    public string Description => "Exposes other stacks' outputs for cross-stack references via fn::open::pulumi-stacks.";

    public EscSchemaSchema Inputs => new()
    {
        Type = "object",
        Required = new List<string> { "organization", "stacks" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["organization"] = new() { Type = "string", Description = "Organization that owns the referenced stacks." },
            ["stacks"] = new() { Type = "object", Description = "Map of output key to { project, stack }.", AdditionalProperties = new() { Type = "object" } },
        },
    };

    public EscSchemaSchema Outputs => new()
    {
        Type = "object",
        Description = "Map of the requested keys to each referenced stack's outputs.",
        AdditionalProperties = new() { Type = "object" },
    };

    public Task<object?> OpenAsync(IReadOnlyDictionary<string, object?> inputs, CancellationToken ct)
    {
        var org = EscProviderInputs.Require<string>(inputs, Name, "organization");
        var stacks = EscProviderInputs.Require<Dictionary<string, object?>>(inputs, Name, "stacks");

        using var scope = scopes.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<IStackOutputsSource>();

        var output = new Dictionary<string, object?>();
        foreach (var (outputKey, spec) in stacks)
        {
            var (project, stack) = ParseSpec(spec, outputKey);
            output[outputKey] = source.Outputs(new StackCoordinates(org, project, stack));
        }
        return Task.FromResult<object?>(output);
    }

    private static (string Project, string Stack) ParseSpec(object? spec, string outputKey)
    {
        if (spec is not Dictionary<string, object?> map
            || map.GetValueOrDefault("project") is not string project
            || map.GetValueOrDefault("stack") is not string stack)
            throw new ArgumentException(
                $"pulumi-stacks 'stacks.{outputKey}' must be an object with 'project' and 'stack'; got {EscProviderInputs.Describe(spec)}.");
        return (project, stack);
    }
}
