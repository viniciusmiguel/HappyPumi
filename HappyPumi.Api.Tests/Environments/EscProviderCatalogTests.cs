using System.Net.Http.Json;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests asserting the registered fn::open provider catalog (Azure, AWS, Vault).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscProviderCatalogTests(HappyPumiApp app)
{
    [Theory]
    [InlineData("azure-keyvault")]
    [InlineData("aws-secrets")]
    [InlineData("vault-secrets")]
    public async Task ListProvidersIncludesAllRegisteredProviders(string provider)
    {
        using var client = app.CreateAuthedClient();
        var response = await client.GetFromJsonAsync<ListProvidersResponse>("/api/esc/providers");
        Assert.Contains(provider, response!.Providers);
    }

    [Theory]
    [InlineData("aws-secrets", "region")]
    [InlineData("vault-secrets", "address")]
    public async Task GetProviderSchemaReturnsRequiredInputs(string provider, string requiredInput)
    {
        using var client = app.CreateAuthedClient();
        var schema = await client.GetFromJsonAsync<ProviderSchema>($"/api/esc/providers/{provider}/schema");
        Assert.Equal(provider, schema!.Name);
        Assert.Contains(requiredInput, schema.Inputs.Required!);
    }
}
