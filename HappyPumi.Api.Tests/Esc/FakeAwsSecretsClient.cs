using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.AwsSecrets;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IAwsSecretsClient"/>: serves secrets from an in-memory map.</summary>
public sealed class FakeAwsSecretsClient : IAwsSecretsClient
{
    private readonly Dictionary<string, string?> _secrets = new();
    public List<AwsSecretRef> Requests { get; } = new();

    public FakeAwsSecretsClient With(string region, string secretId, string? value)
    {
        _secrets[$"{region}|{secretId}"] = value;
        return this;
    }

    public Task<string?> GetSecretAsync(AwsSecretRef reference, CancellationToken ct)
    {
        Requests.Add(reference);
        return Task.FromResult(_secrets.GetValueOrDefault($"{reference.Region}|{reference.SecretId}"));
    }
}
