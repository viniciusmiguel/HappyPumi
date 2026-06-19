using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.AzureKeyVault;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>
/// Named fake (CLAUDE.md) for <see cref="IAzureKeyVaultClient"/>: serves secrets from an in-memory map keyed
/// by <c>vaultUrl|secretName</c> and records each requested reference, so provider tests never touch Azure.
/// </summary>
public sealed class FakeAzureKeyVaultClient : IAzureKeyVaultClient
{
    private readonly Dictionary<string, string?> _secrets = new();

    public List<AzureKeyVaultRef> Requests { get; } = new();

    public FakeAzureKeyVaultClient With(string vaultUrl, string secretName, string? value)
    {
        _secrets[$"{vaultUrl}|{secretName}"] = value;
        return this;
    }

    public Task<string?> GetSecretAsync(AzureKeyVaultRef reference, CancellationToken ct)
    {
        Requests.Add(reference);
        return Task.FromResult(_secrets.GetValueOrDefault($"{reference.VaultUrl}|{reference.SecretName}"));
    }
}
