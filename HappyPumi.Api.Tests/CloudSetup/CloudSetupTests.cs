using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HappyPumi.Api.CloudSetup;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Tests.Esc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HappyPumi.Api.Tests.CloudSetup;

/// <summary>
/// Component tests for the ESC cloud-setup endpoints (PR6) over real Postgres. The default app is
/// provider-unconfigured (URLs empty, lists degrade to empty); <see cref="ConfiguredGcpClient"/> sets the
/// GCP OAuth client and swaps the provider's typed HttpClient handler with a <see cref="StubHttpHandler"/>
/// to drive the configured token-exchange → list-accounts path without a network call.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class CloudSetupTests(HappyPumiApp app)
{
    private const string TokenJson = "{\"access_token\":\"gcp-token\",\"refresh_token\":\"r\"}";
    private const string ProjectsJson =
        "{\"projects\":[{\"projectId\":\"proj-1\",\"name\":\"My Project\",\"projectNumber\":\"42\"}]}";

    private static string NewOrg() => "cloud-" + Guid.NewGuid().ToString("N");

    private HttpClient ConfiguredGcpClient(StubHttpHandler handler)
    {
        var factory = app.WithWebHostBuilder(b =>
        {
            b.UseSetting("CloudSetup:Gcp:ClientId", "gcp-client");
            b.UseSetting("CloudSetup:Gcp:ClientSecret", "gcp-secret");
            b.UseSetting("CloudSetup:Gcp:RedirectUri", "https://happypumi.test/cb");
            b.ConfigureTestServices(s =>
                s.AddHttpClient<GcpCloudSetupProvider>().ConfigurePrimaryHttpMessageHandler(() => handler));
        });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", "dev");
        return client;
    }

    [Fact]
    public async Task InitiateOAuthReturnsSessionAndProviderUrlCarryingState()
    {
        var org = NewOrg();
        using var client = ConfiguredGcpClient(new StubHttpHandler(TokenJson));

        var resp = await client.PostAsJsonAsync(
            $"/api/esc/cloudsetup/{org}/oauth/initiate", new { provider = "gcp" });

        var body = await resp.Content.ReadFromJsonAsync<InitiateOAuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.SessionId));
        Assert.Contains("accounts.google.com", body.Url);
        Assert.Contains($"state={body.SessionId}", body.Url);
    }

    [Fact]
    public async Task InitiateOAuthRejectsUnknownProvider()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();

        var resp = await client.PostAsJsonAsync(
            $"/api/esc/cloudsetup/{org}/oauth/initiate", new { provider = "digitalocean" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CompleteOAuthExchangesCodeAndListAccountsReturnsMappedProjects()
    {
        var org = NewOrg();
        var handler = new StubHttpHandler(TokenJson).ThenRespondWith(ProjectsJson);
        using var client = ConfiguredGcpClient(handler);

        var initiate = await client.PostAsJsonAsync(
            $"/api/esc/cloudsetup/{org}/oauth/initiate", new { provider = "gcp" });
        var session = (await initiate.Content.ReadFromJsonAsync<InitiateOAuthResponse>())!.SessionId;

        var complete = await client.PostAsJsonAsync(
            $"/api/esc/cloudsetup/{org}/oauth/complete", new { sessionID = session, code = "the-code" });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        Assert.Equal(2, handler.CallCount); // token-exchange POST + list-accounts GET

        var accounts = await client.GetFromJsonAsync<ListCloudAccountsResponse>(
            $"/api/esc/cloudsetup/{org}/oauth/gcp/accounts");
        var project = Assert.Single(accounts!.Accounts);
        Assert.Equal("proj-1", project.Id);
        Assert.Equal("My Project", project.Name);
        Assert.Equal(42, project.Number);
    }

    [Fact]
    public async Task AzureSetupReturnsSuccess()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();

        var result = await PostSetup(client, $"/api/esc/cloudsetup/{org}/oauth/azure/setup");
        Assert.True(result.Success);
    }

    [Fact]
    public async Task GcpSetupReturnsSuccess()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();

        var result = await PostSetup(client, $"/api/esc/cloudsetup/{org}/oauth/gcp/setup");
        Assert.True(result.Success);
    }

    [Fact]
    public async Task AwsSetupReturnsSuccess()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();

        var result = await PostSetup(client, $"/api/esc/cloudsetup/{org}/aws/setup");
        Assert.True(result.Success);
    }

    [Fact]
    public async Task AwsssoSetupReturnsSuccess()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();

        var result = await PostSetup(client, $"/api/esc/cloudsetup/{org}/aws/sso/setup");
        Assert.True(result.Success);
    }

    [Fact]
    public async Task AwsssoInitiateReturnsSessionUrlAndUserCode()
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();

        var resp = await client.PostAsJsonAsync(
            $"/api/esc/cloudsetup/{org}/aws/sso/initiate", new { startUrl = "https://acme.awsapps.com/start" });

        var body = await resp.Content.ReadFromJsonAsync<AwsssoInitiateResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.SessionId));
        Assert.Contains("acme.awsapps.com", body.Url);
        Assert.False(string.IsNullOrWhiteSpace(body.UserCode));
    }

    [Theory]
    [InlineData("aws/sso/accounts")]
    [InlineData("oauth/azure/accounts")]
    [InlineData("oauth/gcp/accounts")]
    public async Task ListAccountsIsEmptyWhenNothingConnected(string path)
    {
        var org = NewOrg();
        using var client = app.CreateAuthedClient();

        var resp = await client.GetAsync($"/api/esc/cloudsetup/{org}/{path}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<ListCloudAccountsResponse>();
        Assert.Empty(body!.Accounts);
    }

    private static async Task<CloudSetupResult> PostSetup(HttpClient client, string path)
    {
        var resp = await client.PostAsJsonAsync(path, new { });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<CloudSetupResult>())!;
    }
}
