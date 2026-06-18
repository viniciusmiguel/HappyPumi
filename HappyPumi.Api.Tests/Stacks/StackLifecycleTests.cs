using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for the Tier-1a stack + config lifecycle (ENDPOINTS.md). These drive the same
/// routes the CLI uses for `pulumi stack init` / `rm` and service-managed config. The store is shared
/// across the collection, so each test uses a unique stack name to stay independent.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackLifecycleTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";

    private static string Base(string stack) => $"/api/stacks/{Org}/{Project}/{stack}";

    private static async Task<HttpResponseMessage> CreateStack(HttpClient client, string stack) =>
        await client.PostAsJsonAsync($"/api/stacks/{Org}/{Project}",
            new AppCreateStackRequest { StackName = stack });

    [Fact]
    public async Task CreateThenGetRoundTripsTheStack()
    {
        using var client = app.CreateClient();
        var stack = $"create-{Guid.NewGuid():N}";

        using var created = await CreateStack(client, stack);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var fetched = await client.GetFromJsonAsync<AppStack>(Base(stack));
        Assert.NotNull(fetched);
        Assert.Equal(stack, fetched!.StackName);
        Assert.Equal(Org, fetched.OrgName);
        Assert.Equal($"{Org}/{Project}/{stack}", fetched.Id);
        Assert.Equal(0, fetched.Version);
    }

    [Fact]
    public async Task GetUnknownStackReturns404()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync(Base($"missing-{Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDuplicateStackReturns409()
    {
        using var client = app.CreateClient();
        var stack = $"dup-{Guid.NewGuid():N}";
        (await CreateStack(client, stack)).Dispose();

        using var second = await CreateStack(client, stack);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateWithoutStackNameReturns400()
    {
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/stacks/{Org}/{Project}", new AppCreateStackRequest { StackName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProjectExistsReflectsStackCreation()
    {
        using var client = app.CreateClient();
        var project = $"proj-{Guid.NewGuid():N}";

        using var before = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Head, $"/api/stacks/{Org}/{project}"));
        Assert.Equal(HttpStatusCode.NotFound, before.StatusCode);

        using var _ = await client.PostAsJsonAsync(
            $"/api/stacks/{Org}/{project}", new AppCreateStackRequest { StackName = "dev" });

        using var after = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Head, $"/api/stacks/{Org}/{project}"));
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
    }

    [Fact]
    public async Task ConfigPutGetDeleteRoundTrips()
    {
        using var client = app.CreateClient();
        var stack = $"cfg-{Guid.NewGuid():N}";
        (await CreateStack(client, stack)).Dispose();

        using var put = await client.PutAsJsonAsync(
            $"{Base(stack)}/config", new AppStackConfig { SecretsProvider = "passphrase" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var fetched = await client.GetFromJsonAsync<AppStackConfig>($"{Base(stack)}/config");
        Assert.Equal("passphrase", fetched?.SecretsProvider);

        using var deleted = await client.DeleteAsync($"{Base(stack)}/config");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var afterDelete = await client.GetAsync($"{Base(stack)}/config");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task GetConfigReturns404WhenUnset()
    {
        using var client = app.CreateClient();
        var stack = $"nocfg-{Guid.NewGuid():N}";
        (await CreateStack(client, stack)).Dispose();

        using var response = await client.GetAsync($"{Base(stack)}/config");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteStackReturns204ThenGetIs404()
    {
        using var client = app.CreateClient();
        var stack = $"del-{Guid.NewGuid():N}";
        (await CreateStack(client, stack)).Dispose();

        using var deleted = await client.DeleteAsync(Base(stack));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var afterDelete = await client.GetAsync(Base(stack));
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    // A freshly created stack has no update history yet (the lifecycle lands in Tier 1c), so the CLI
    // must see 404 "never updated" rather than an error.
    [Fact]
    public async Task GetLatestUpdateReturns404ForFreshStack()
    {
        using var client = app.CreateClient();
        var stack = $"upd-{Guid.NewGuid():N}";
        (await CreateStack(client, stack)).Dispose();

        using var response = await client.GetAsync($"{Base(stack)}/updates/latest");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
