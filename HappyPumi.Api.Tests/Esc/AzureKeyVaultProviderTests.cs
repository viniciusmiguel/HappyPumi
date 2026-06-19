using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.AzureKeyVault;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the fn::open::azure-keyvault provider (against a fake key-vault client).</summary>
public sealed class AzureKeyVaultProviderTests
{
    private const string VaultUrl = "https://my-vault.vault.azure.net";

    [Fact]
    public async Task ReadsNamedSecretsAndMarksThemSecret()
    {
        var client = new FakeAzureKeyVaultClient().With(VaultUrl, "api-key", "s3cr3t");
        var provider = new AzureKeyVaultProvider(client);
        var inputs = new Dictionary<string, object?>
        {
            ["vault"] = VaultUrl,
            ["get"] = new Dictionary<string, object?>
            {
                ["apiKey"] = new Dictionary<string, object?> { ["name"] = "api-key" },
            },
        };

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(inputs, CancellationToken.None))!;

        // Each output is wrapped as { fn::secret: <value> } so the evaluator flags it.
        var apiKey = (Dictionary<string, object?>)output["apiKey"]!;
        Assert.Equal("s3cr3t", apiKey["fn::secret"]);
    }

    [Fact]
    public async Task ResolvesBareVaultNameToAzureUrl()
    {
        var client = new FakeAzureKeyVaultClient().With(VaultUrl, "db-password", "pw");
        var provider = new AzureKeyVaultProvider(client);
        var inputs = new Dictionary<string, object?>
        {
            ["vault"] = "my-vault", // bare name, no scheme
            ["get"] = new Dictionary<string, object?>
            {
                ["dbPassword"] = new Dictionary<string, object?> { ["name"] = "db-password" },
            },
        };

        await provider.OpenAsync(inputs, CancellationToken.None);

        Assert.Single(client.Requests);
        Assert.Equal(VaultUrl, client.Requests[0].VaultUrl);
    }

    [Fact]
    public async Task MissingVaultThrowsWithOffendingShape()
    {
        var provider = new AzureKeyVaultProvider(new FakeAzureKeyVaultClient());
        var inputs = new Dictionary<string, object?> { ["get"] = new Dictionary<string, object?>() };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("vault", ex.Message);
    }

    [Fact]
    public async Task SecretSpecWithoutNameThrows()
    {
        var provider = new AzureKeyVaultProvider(new FakeAzureKeyVaultClient());
        var inputs = new Dictionary<string, object?>
        {
            ["vault"] = VaultUrl,
            ["get"] = new Dictionary<string, object?> { ["apiKey"] = new Dictionary<string, object?>() },
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("apiKey", ex.Message);
    }
}
