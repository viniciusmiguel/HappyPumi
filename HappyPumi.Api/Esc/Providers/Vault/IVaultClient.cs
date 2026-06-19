#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.Vault;

/// <summary>A single Vault KV-v2 read: server address + token, the KV mount, the secret path, and the field.</summary>
public readonly record struct VaultSecretRef(string Address, string? Token, string Mount, string Path, string Field);

/// <summary>
/// Owned, thin seam over HashiCorp Vault's KV-v2 HTTP API (CLAUDE.md: wrap third-party access). Lets the
/// <see cref="VaultSecretsProvider"/> be unit-tested with a named fake instead of calling a live Vault.
/// </summary>
public interface IVaultClient
{
    /// <summary>Reads one field of a KV-v2 secret at open time. Returns null when the field is absent.</summary>
    Task<string?> ReadAsync(VaultSecretRef reference, CancellationToken ct);
}
