using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.Vault;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the fn::open::vault-secrets provider (against a fake Vault client).</summary>
public sealed class VaultSecretsProviderTests
{
    private const string Address = "https://vault.example.com";

    [Fact]
    public async Task ReadsFieldAndMarksItSecret()
    {
        var client = new FakeVaultClient().With("secret", "app/config", "api_key", "vlt-123");
        var provider = new VaultSecretsProvider(client);
        var inputs = new Dictionary<string, object?>
        {
            ["address"] = Address,
            ["get"] = new Dictionary<string, object?>
            {
                ["apiKey"] = new Dictionary<string, object?> { ["path"] = "app/config", ["field"] = "api_key" },
            },
        };

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(inputs, CancellationToken.None))!;

        var apiKey = (Dictionary<string, object?>)output["apiKey"]!;
        Assert.Equal("vlt-123", apiKey["fn::secret"]);
        Assert.Equal("secret", client.Requests.Single().Mount); // default mount
    }

    [Fact]
    public async Task MissingAddressThrows()
    {
        var provider = new VaultSecretsProvider(new FakeVaultClient());
        var inputs = new Dictionary<string, object?> { ["get"] = new Dictionary<string, object?>() };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("address", ex.Message);
    }

    [Fact]
    public async Task SpecWithoutPathOrFieldThrows()
    {
        var provider = new VaultSecretsProvider(new FakeVaultClient());
        var inputs = new Dictionary<string, object?>
        {
            ["address"] = Address,
            ["get"] = new Dictionary<string, object?> { ["apiKey"] = new Dictionary<string, object?> { ["path"] = "app/config" } },
        };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("apiKey", ex.Message);
    }
}
