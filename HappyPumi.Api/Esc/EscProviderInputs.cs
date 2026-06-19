#nullable enable

using System;
using System.Collections.Generic;

namespace HappyPumi.Api.Esc;

/// <summary>
/// Shared helpers for parsing <c>fn::open</c> provider inputs, so each <see cref="IEscProvider"/> validates
/// the same way and wraps secret outputs consistently (as <c>{ "fn::secret": value }</c> for the evaluator).
/// </summary>
public static class EscProviderInputs
{
    /// <summary>Reads a required input of the expected type, or throws with the offending value and provider name.</summary>
    public static T Require<T>(IReadOnlyDictionary<string, object?> inputs, string provider, string key) where T : class
        => inputs.GetValueOrDefault(key) as T
           ?? throw new ArgumentException(
               $"{provider} requires '{key}' of type {typeof(T).Name}; got {Describe(inputs.GetValueOrDefault(key))}.");

    /// <summary>Wraps a resolved value so the evaluator flags it secret.</summary>
    public static Dictionary<string, object?> Secret(object? value) => new() { ["fn::secret"] = value };

    public static string Describe(object? value) => value is null ? "null" : $"{value.GetType().Name} ({value})";
}
