#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Esc.Providers.Logins;

/// <summary>One credential field a login provider accepts.</summary>
public readonly record struct LoginField(string Key, bool Secret, bool Required);

/// <summary>
/// Base for the <c>fn::open::&lt;cloud&gt;-login</c> credential providers. It exposes the supplied credentials
/// at open time (sensitive fields wrapped as <c>fn::secret</c>) so they can be composed into the matching
/// secrets provider (e.g. <c>aws-secrets</c> <c>login: ${aws.login}</c>).
/// </summary>
/// <remarks>
/// This base implements the <em>static credential</em> mode: it passes through the credentials in the
/// definition. Providers that support <em>OIDC federation</em> (vending short-lived credentials from a
/// trusted token via the cloud's identity broker) override <see cref="OpenAsync"/> — see
/// <c>AwsLoginProvider</c>.
/// </remarks>
public abstract class StaticLoginProvider : IEscProvider
{
    protected abstract IReadOnlyList<LoginField> Fields { get; }
    protected abstract string Cloud { get; }

    public abstract string Name { get; }

    public virtual string Description => $"Provides {Cloud} credentials at open time (static credentials).";

    public virtual EscSchemaSchema Inputs => new()
    {
        Type = "object",
        Required = Fields.Where(f => f.Required).Select(f => f.Key).ToList(),
        Properties = Fields.ToDictionary(f => f.Key, f => new EscSchemaSchema { Type = "string", Secret = f.Secret }),
    };

    public EscSchemaSchema Outputs => new()
    {
        Type = "object",
        Description = "The login credentials.",
        AdditionalProperties = new() { Type = "string", Secret = true },
    };

    public virtual Task<object?> OpenAsync(IReadOnlyDictionary<string, object?> inputs, CancellationToken ct)
    {
        var output = new Dictionary<string, object?>();
        foreach (var field in Fields)
        {
            var value = inputs.GetValueOrDefault(field.Key);
            if (value is null)
            {
                if (field.Required)
                    throw new ArgumentException($"{Name} requires '{field.Key}'.");
                continue;
            }
            output[field.Key] = field.Secret ? EscProviderInputs.Secret(value) : value;
        }
        return Task.FromResult<object?>(output);
    }
}
