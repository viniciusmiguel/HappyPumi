using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Deployments;

/// <summary>
/// Component tests for Tier 6 managed deployments (ENDPOINTS.md): deployment settings, deployments,
/// schedules, drift status, and webhooks. Unique stack per test (shared store).
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class DeploymentsTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";

    private static string Base(string stack) => $"/api/stacks/{Org}/{Project}/{stack}";
    private static string Stack() => $"dep-{Guid.NewGuid():N}";

    [Fact]
    public async Task SettingsUpsertGetDelete()
    {
        using var client = app.CreateClient();
        var stack = Stack();

        using var before = await client.GetAsync($"{Base(stack)}/deployments/settings");
        Assert.Equal(HttpStatusCode.NotFound, before.StatusCode);

        using var put = await client.PutAsJsonAsync($"{Base(stack)}/deployments/settings",
            new DeploymentSettingsRequest { AgentPoolId = "pool-1", Tag = "prod" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var settings = await client.GetFromJsonAsync<DeploymentSettings>($"{Base(stack)}/deployments/settings");
        Assert.Equal("pool-1", settings!.AgentPoolId);
        Assert.Equal("prod", settings.Tag);

        using var deleted = await client.DeleteAsync($"{Base(stack)}/deployments/settings");
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        using var after = await client.GetAsync($"{Base(stack)}/deployments/settings");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task EncryptSecretReturnsCiphertext()
    {
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync($"{Base(Stack())}/deployments/settings/encrypt",
            new SecretValue { Secret = "hunter2" });
        response.EnsureSuccessStatusCode();
        var secret = await response.Content.ReadFromJsonAsync<SecretValue>();

        Assert.NotNull(secret!.Ciphertext);
        Assert.NotEmpty(secret.Ciphertext!);
    }

    [Fact]
    public async Task CreateListCancelDeployment()
    {
        using var client = app.CreateClient();
        var stack = Stack();

        var created = await Post<CreateDeploymentResponse>(client, $"{Base(stack)}/deployments",
            new CreateDeploymentRequest { Operation = "update" });
        Assert.False(string.IsNullOrEmpty(created.Id));
        Assert.Equal(1, created.Version);

        var list = await client.GetFromJsonAsync<ListDeploymentResponseV2>($"{Base(stack)}/deployments");
        Assert.Equal(1, list!.Total);

        using var cancel = await client.PostAsJsonAsync($"{Base(stack)}/deployments/{created.Id}/cancel", new { });
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        using var cancelAgain = await client.PostAsJsonAsync($"{Base(stack)}/deployments/{created.Id}/cancel", new { });
        Assert.Equal(HttpStatusCode.NotFound, cancelAgain.StatusCode);
    }

    [Fact]
    public async Task ScheduleCreateAndList()
    {
        using var client = app.CreateClient();
        var stack = Stack();

        var schedule = await Post<ScheduledAction>(client, $"{Base(stack)}/deployments/schedules", new { });
        Assert.Equal("deployment", schedule.Kind);

        var list = await client.GetFromJsonAsync<ListScheduledActionsResponse>($"{Base(stack)}/deployments/schedules");
        Assert.Single(list!.Schedules);
    }

    [Fact]
    public async Task DriftStatusReportsNoDrift()
    {
        using var client = app.CreateClient();

        var status = await client.GetFromJsonAsync<StackDriftStatus>($"{Base(Stack())}/drift/status");

        Assert.False(status!.DriftDetected);
    }

    [Fact]
    public async Task WebhookCreateAndList()
    {
        using var client = app.CreateClient();
        var stack = Stack();

        var created = await Post<WebhookResponse>(client, $"{Base(stack)}/hooks",
            new Webhook { Name = "ci", DisplayName = "CI", PayloadUrl = "https://example.invalid/hook" });
        Assert.Equal("ci", created.Name);

        var list = await client.GetFromJsonAsync<List<WebhookResponse>>($"{Base(stack)}/hooks");
        Assert.Single(list!);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
