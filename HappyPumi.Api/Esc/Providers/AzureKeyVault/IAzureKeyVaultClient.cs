#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.AzureKeyVault;

/// <summary>Optional explicit service-principal credentials for a key-vault read (else ambient creds are used).</summary>
public sealed record AzureKeyVaultLogin(string? ClientId, string? ClientSecret, string? TenantId);

/// <summary>A single secret to fetch: vault URL + secret name (+ optional version + optional login).</summary>
public readonly record struct AzureKeyVaultRef(string VaultUrl, string SecretName, string? Version, AzureKeyVaultLogin? Login);

/// <summary>
/// Owned, thin seam over the Azure Key Vault Secrets SDK (CLAUDE.md: wrap third-party libs). Lets the
/// <see cref="AzureKeyVaultProvider"/> be unit-tested with a named fake instead of hitting Azure.
/// </summary>
public interface IAzureKeyVaultClient
{
    /// <summary>Reads a secret's value at open time. Returns null when the secret has no value.</summary>
    Task<string?> GetSecretAsync(AzureKeyVaultRef reference, CancellationToken ct);
}
