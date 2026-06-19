using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Component tests for the RotateEnvironment + rotation-history endpoints.</summary>
[Collection(HappyPumiCollection.Name)]
public sealed class EscRotationEndpointTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "esc-rotate";

    [Fact]
    public async Task RotateWithNoDeclarationsSucceedsAndIsRecorded()
    {
        using var client = app.CreateAuthedClient();
        var name = await CreateEnv(client);

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var res = await client.PostAsync($"{EnvUrl(name)}/rotate", content);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<RotateEnvironmentResponse>();
        Assert.Equal("succeeded", body!.SecretRotationEvent.Status);
        Assert.Empty(body.SecretRotationEvent.Rotations);

        var history = await client.GetFromJsonAsync<ListEnvironmentSecretRotationHistoryResponse>($"{EnvUrl(name)}/rotate/history");
        Assert.Contains(history!.Events, e => e.Id == body.Id);
    }

    [Fact]
    public async Task RotateMissingEnvironmentReturns404()
    {
        using var client = app.CreateAuthedClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var res = await client.PostAsync($"{EnvUrl($"missing-{Guid.NewGuid():N}")}/rotate", content);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
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
