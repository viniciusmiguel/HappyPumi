using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for the ESC rotator catalog (fn::rotate rotators).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscRotatorCatalogTests(HappyPumiApp app)
{
    [Fact]
    public async Task ListRotatorsIncludesAwsIam()
    {
        using var client = app.CreateAuthedClient();
        var response = await client.GetFromJsonAsync<ListRotatorsResponse>("/api/esc/rotators");
        Assert.Contains("aws-iam", response!.Rotators);
    }

    [Fact]
    public async Task GetRotatorSchemaReturnsInputsForAwsIam()
    {
        using var client = app.CreateAuthedClient();
        var schema = await client.GetFromJsonAsync<ProviderSchema>("/api/esc/rotators/aws-iam/schema");
        Assert.Equal("aws-iam", schema!.Name);
        Assert.Contains("region", schema.Inputs.Required!);
    }

    [Fact]
    public async Task GetRotatorSchemaForUnknownReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.GetAsync("/api/esc/rotators/no-such-rotator/schema");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
