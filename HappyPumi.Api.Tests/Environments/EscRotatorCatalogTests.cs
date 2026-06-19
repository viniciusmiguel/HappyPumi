using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for the ESC rotator catalog (fn::rotate rotators).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscRotatorCatalogTests(HappyPumiApp app)
{
    [Theory]
    [InlineData("aws-iam")]
    [InlineData("postgres")]
    public async Task ListRotatorsIncludesRegisteredRotators(string rotator)
    {
        using var client = app.CreateAuthedClient();
        var response = await client.GetFromJsonAsync<ListRotatorsResponse>("/api/esc/rotators");
        Assert.Contains(rotator, response!.Rotators);
    }

    [Theory]
    [InlineData("aws-iam", "region")]
    [InlineData("postgres", "host")]
    public async Task GetRotatorSchemaReturnsRequiredInputs(string rotator, string requiredInput)
    {
        using var client = app.CreateAuthedClient();
        var schema = await client.GetFromJsonAsync<ProviderSchema>($"/api/esc/rotators/{rotator}/schema");
        Assert.Equal(rotator, schema!.Name);
        Assert.Contains(requiredInput, schema.Inputs.Required!);
    }

    [Fact]
    public async Task GetRotatorSchemaForUnknownReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.GetAsync("/api/esc/rotators/no-such-rotator/schema");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
