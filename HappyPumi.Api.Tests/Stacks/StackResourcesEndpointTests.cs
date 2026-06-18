using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>Component tests for the stack Resources tab endpoints (resources/latest + resources/count).</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackResourcesEndpointTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";

    private static async Task<string> NewStack(HttpClient client)
    {
        var stack = $"res-{Guid.NewGuid():N}";
        using var res = await client.PostAsJsonAsync($"/api/stacks/{Org}/{Project}",
            new AppCreateStackRequest { StackName = stack });
        res.EnsureSuccessStatusCode();
        return stack;
    }

    [Fact]
    public async Task FreshStackHasZeroResources()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);

        var count = await client.GetFromJsonAsync<GetStackResourceCountResponse>(
            $"/api/stacks/{Org}/{Project}/{stack}/resources/count");
        var latest = await client.GetFromJsonAsync<GetStackResourcesResponse>(
            $"/api/stacks/{Org}/{Project}/{stack}/resources/latest");

        Assert.Equal(0, count!.ResourceCount);
        Assert.Empty(latest!.Resources);
    }

    [Fact]
    public async Task UnknownStackReturns404()
    {
        using var client = app.CreateClient();

        using var count = await client.GetAsync($"/api/stacks/{Org}/{Project}/missing-{Guid.NewGuid():N}/resources/count");
        using var latest = await client.GetAsync($"/api/stacks/{Org}/{Project}/missing-{Guid.NewGuid():N}/resources/latest");

        Assert.Equal(HttpStatusCode.NotFound, count.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, latest.StatusCode);
    }
}
