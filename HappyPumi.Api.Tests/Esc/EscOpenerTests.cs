using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.Esc.Providers.AzureKeyVault;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>
/// Unit tests for the full open path: imports + interpolation + fn::open::azure-keyvault, end to end through
/// <see cref="EscOpener"/> with a fake store and a fake key-vault client.
/// </summary>
public sealed class EscOpenerTests
{
    private const string VaultUrl = "https://v.vault.azure.net";
    private static readonly EnvCoordinates App = new("org", "proj", "app");
    private static readonly EnvCoordinates Base = new("org", "proj", "base");

    private static EscOpener Build(FakeAzureKeyVaultClient client, FakeEnvironmentStore store)
    {
        var registry = new EscProviderRegistry(new IEscProvider[] { new AzureKeyVaultProvider(client) });
        return new EscOpener(store, registry);
    }

    [Fact]
    public async Task OpensImportProviderAndInterpolationTogether()
    {
        var client = new FakeAzureKeyVaultClient().With(VaultUrl, "api-key", "s3cr3t");
        var store = new FakeEnvironmentStore()
            .With(Base, $"values:\n  azure:\n    vaultUrl: {VaultUrl}\n");
        const string appYaml = """
        imports:
          - base
        values:
          kv:
            fn::open::azure-keyvault:
              vault: ${azure.vaultUrl}
              get:
                apiKey:
                  name: api-key
          ref: ${kv.apiKey}
        """;

        var props = await Build(client, store).OpenAsync(App, appYaml, CancellationToken.None);

        var kv = (Dictionary<string, EscValue>)props["kv"].Value!;
        Assert.Equal("s3cr3t", kv["apiKey"].Value);
        Assert.True(kv["apiKey"].Secret);            // provider value flagged secret
        Assert.Equal("s3cr3t", props["ref"].Value);  // interpolation references the opened provider output
    }

    [Fact]
    public async Task UnknownProviderIsLeftUnresolved()
    {
        var store = new FakeEnvironmentStore();
        const string yaml = """
        values:
          x:
            fn::open::no-such-provider:
              foo: bar
        """;

        var props = await Build(new FakeAzureKeyVaultClient(), store).OpenAsync(App, yaml, CancellationToken.None);

        Assert.True(props["x"].Unknown);
    }
}
