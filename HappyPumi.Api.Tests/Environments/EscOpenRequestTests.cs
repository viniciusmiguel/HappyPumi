using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for the gated-open access-request workflow.</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscOpenRequestTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-openreq";

    [Fact]
    public async Task OpenRequestCreateReadUpdateLifecycle()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        var created = await (await client.PostAsJsonAsync($"{EnvUrl(name)}/open/request",
            new CreateEnvironmentOpenRequest { AccessDurationSeconds = 3600, GrantExpirationSeconds = 7200 }))
            .Content.ReadFromJsonAsync<CreateEnvironmentOpenRequestResponse>();
        var change = Assert.Single(created!.ChangeRequests);
        Assert.False(string.IsNullOrEmpty(change.ChangeRequestId));
        Assert.Equal(name, change.EnvironmentName);
        var id = change.ChangeRequestId;

        var read = await client.GetFromJsonAsync<CreateEnvironmentOpenRequest>($"{EnvUrl(name)}/open/request/{id}");
        Assert.Equal(3600, read!.AccessDurationSeconds);

        using var put = await client.PutAsJsonAsync($"{EnvUrl(name)}/open/request/{id}",
            new CreateEnvironmentOpenRequest { AccessDurationSeconds = 1800, GrantExpirationSeconds = 7200 });
        put.EnsureSuccessStatusCode();
        var updated = await client.GetFromJsonAsync<CreateEnvironmentOpenRequest>($"{EnvUrl(name)}/open/request/{id}");
        Assert.Equal(1800, updated!.AccessDurationSeconds);
    }

    [Fact]
    public async Task CreateForMissingEnvironmentReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var res = await client.PostAsJsonAsync($"{EnvUrl($"missing-{Guid.NewGuid():N}")}/open/request",
            new CreateEnvironmentOpenRequest { AccessDurationSeconds = 60 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task ReadUnknownRequestReturns404()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{EnvUrl(name)}/open/request/bogus")).StatusCode);
    }

    private static string EnvUrl(string name) => $"/api/esc/environments/{Org}/{Project}/{name}";

    private static async Task<string> CreateEnv(HttpClient client)
    {
        var name = $"env-{Guid.NewGuid():N}";
        using var res = await client.PostAsJsonAsync($"/api/esc/environments/{Org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });
        res.EnsureSuccessStatusCode();
        return name;
    }
}
