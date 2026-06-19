using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.Logins.Azure;
using HappyPumi.Api.Esc.Providers.Logins.Gcp;
using HappyPumi.Api.Esc.Providers.Logins.Vault;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Records the Azure client-assertion request and returns a canned access token.</summary>
public sealed class FakeAzureOidcExchanger : IAzureOidcExchanger
{
    public AzureClientAssertionRequest? LastRequest { get; private set; }

    public Task<string> ExchangeForAccessTokenAsync(AzureClientAssertionRequest request, CancellationToken ct)
    {
        LastRequest = request;
        return Task.FromResult("azure-access-token");
    }
}

/// <summary>Records the GCP federation request and returns a canned access token.</summary>
public sealed class FakeGcpOidcExchanger : IGcpOidcExchanger
{
    public GcpFederationRequest? LastRequest { get; private set; }

    public Task<string> ExchangeForAccessTokenAsync(GcpFederationRequest request, CancellationToken ct)
    {
        LastRequest = request;
        return Task.FromResult("gcp-access-token");
    }
}

/// <summary>Records the Vault JWT login request and returns a canned client token.</summary>
public sealed class FakeVaultJwtExchanger : IVaultJwtExchanger
{
    public VaultJwtLoginRequest? LastRequest { get; private set; }

    public Task<string> LoginAsync(VaultJwtLoginRequest request, CancellationToken ct)
    {
        LastRequest = request;
        return Task.FromResult("vault-client-token");
    }
}
