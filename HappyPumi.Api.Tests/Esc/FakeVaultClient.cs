using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.Vault;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IVaultClient"/>: serves fields from an in-memory map.</summary>
public sealed class FakeVaultClient : IVaultClient
{
    private readonly Dictionary<string, string?> _values = new();
    public List<VaultSecretRef> Requests { get; } = new();

    public FakeVaultClient With(string mount, string path, string field, string? value)
    {
        _values[$"{mount}|{path}|{field}"] = value;
        return this;
    }

    public Task<string?> ReadAsync(VaultSecretRef reference, CancellationToken ct)
    {
        Requests.Add(reference);
        return Task.FromResult(_values.GetValueOrDefault($"{reference.Mount}|{reference.Path}|{reference.Field}"));
    }
}
