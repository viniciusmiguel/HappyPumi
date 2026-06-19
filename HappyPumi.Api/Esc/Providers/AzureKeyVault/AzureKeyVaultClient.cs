#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace HappyPumi.Api.Esc.Providers.AzureKeyVault;

/// <summary>
/// Real <see cref="IAzureKeyVaultClient"/> backed by <c>Azure.Security.KeyVault.Secrets</c>. Credentials are
/// resolved just-in-time per ESC: an explicit service principal when the definition supplies one, otherwise
/// <see cref="DefaultAzureCredential"/> (managed identity / workload identity / env vars). Nothing is cached.
/// </summary>
public sealed class AzureKeyVaultClient : IAzureKeyVaultClient
{
    public async Task<string?> GetSecretAsync(AzureKeyVaultRef reference, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reference.VaultUrl))
            throw new ArgumentException("azure-keyvault requires a 'vault' URL.", nameof(reference));

        var client = new SecretClient(new Uri(reference.VaultUrl), CredentialFor(reference.Login));
        var response = await client.GetSecretAsync(reference.SecretName, reference.Version, ct);
        return response.Value.Value;
    }

    // Explicit SP creds when the definition provides client/secret/tenant; ambient creds otherwise.
    private static Azure.Core.TokenCredential CredentialFor(AzureKeyVaultLogin? login) =>
        login is { ClientId: { } id, ClientSecret: { } secret, TenantId: { } tenant }
            && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(secret) && !string.IsNullOrWhiteSpace(tenant)
            ? new ClientSecretCredential(tenant, id, secret)
            : new DefaultAzureCredential();
}
