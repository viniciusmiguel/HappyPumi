using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.GcpSecrets;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IGcpSecretsClient"/>: serves payloads from an in-memory map.</summary>
public sealed class FakeGcpSecretsClient : IGcpSecretsClient
{
    private readonly Dictionary<string, string?> _secrets = new();
    public List<GcpSecretRef> Requests { get; } = new();

    public FakeGcpSecretsClient With(string projectId, string secretId, string? value)
    {
        _secrets[$"{projectId}|{secretId}"] = value;
        return this;
    }

    public Task<string?> AccessAsync(GcpSecretRef reference, CancellationToken ct)
    {
        Requests.Add(reference);
        return Task.FromResult(_secrets.GetValueOrDefault($"{reference.ProjectId}|{reference.SecretId}"));
    }
}
