#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Environments;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Esc;

/// <summary>
/// Opens an ESC environment: loads it and (recursively) its imports, deep-merges their <c>values</c>, runs
/// every <c>fn::open::&lt;provider&gt;</c> against the registered providers, then evaluates interpolation,
/// built-ins and <c>fn::secret</c> into the resolved property tree the session returns. This is the full
/// <em>open</em> semantics (vs. <c>check</c>, which leaves providers unresolved/unknown).
/// </summary>
public sealed class EscOpener(IEnvironmentStore environments, IEscProviderRegistry providers)
{
    /// <summary>Evaluates an environment definition to its fully resolved <c>properties</c> tree.</summary>
    public async Task<Dictionary<string, EscValue>> OpenAsync(EnvCoordinates coords, string yaml, CancellationToken ct)
    {
        var root = EnvironmentEvaluator.ParseRoot(yaml);
        var merged = EnvironmentImporter.MergedValues(coords, root, ResolveRoot);
        await ResolveProvidersAsync(merged, merged, ct);
        return EnvironmentEvaluator.EvaluateValues(merged);
    }

    // Imports are loaded from the store and parsed; missing imports resolve to null (skipped).
    private Dictionary<string, object?>? ResolveRoot(EnvCoordinates coords)
    {
        var env = environments.Get(coords);
        return env is null ? null : EnvironmentEvaluator.ParseRoot(env.Yaml);
    }

    // Walk the merged tree depth-first, replacing each fn::open node with its provider's resolved output.
    private async Task ResolveProvidersAsync(Dictionary<string, object?> map, Dictionary<string, object?> root, CancellationToken ct)
    {
        foreach (var key in map.Keys.ToList())
            map[key] = await ResolveNodeAsync(map[key], root, ct);
    }

    private async Task<object?> ResolveNodeAsync(object? node, Dictionary<string, object?> root, CancellationToken ct)
    {
        if (node is Dictionary<string, object?> map && TryReadOpen(map, out var providerName, out var inputsNode))
            return await InvokeProviderAsync(providerName, inputsNode, root, ct);

        if (node is Dictionary<string, object?> obj)
        {
            await ResolveProvidersAsync(obj, root, ct);
            return obj;
        }
        if (node is List<object?> list)
        {
            for (var i = 0; i < list.Count; i++)
                list[i] = await ResolveNodeAsync(list[i], root, ct);
            return list;
        }
        return node;
    }

    // Run the provider with its interpolated inputs; an unknown provider is left unresolved (-> Unknown).
    private async Task<object?> InvokeProviderAsync(string providerName, object? inputsNode, Dictionary<string, object?> root, CancellationToken ct)
    {
        if (!providers.TryGet(providerName, out var provider))
            return new Dictionary<string, object?> { [$"fn::open::{providerName}"] = inputsNode };

        var inputs = EnvironmentEvaluator.ResolveNode(inputsNode, root) as Dictionary<string, object?>
                     ?? new Dictionary<string, object?>();
        return await provider.OpenAsync(inputs, ct);
    }

    // Recognize fn::open::<name> (inputs = arg) and the generic fn::open ({ provider, inputs }).
    private static bool TryReadOpen(Dictionary<string, object?> map, out string providerName, out object? inputs)
    {
        providerName = "";
        inputs = null;
        if (map.Count != 1)
            return false;
        var key = map.Keys.First();

        if (key.StartsWith("fn::open::"))
        {
            providerName = key["fn::open::".Length..];
            inputs = map[key];
            return true;
        }
        if (key == "fn::open" && map[key] is Dictionary<string, object?> generic && generic.GetValueOrDefault("provider") is string name)
        {
            providerName = name;
            inputs = generic.GetValueOrDefault("inputs");
            return true;
        }
        return false;
    }
}
