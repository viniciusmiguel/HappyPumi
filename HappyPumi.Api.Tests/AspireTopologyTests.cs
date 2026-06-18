using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Logging;

namespace HappyPumi.Api.Tests;

/// <summary>
/// Boots the full Aspire topology (Postgres + API) the same way `dotnet run --project HappyPumi.AppHost`
/// does, and asserts the API comes up healthy and serves traffic (ADR-0003). This is the slow,
/// Docker-backed smoke test — it pulls/starts the Postgres container — so the bulk of behavior is
/// covered by the fast in-process component tests instead.
///
/// Requires a running Docker daemon. Skips automatically when Docker is unavailable so the suite
/// stays green on machines/CI without it.
/// </summary>
public sealed class AspireTopologyTests
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    [SkippableFact]
    public async Task ApiBecomesHealthyAndServesCurrentUser()
    {
        Skip.IfNot(DockerAvailable.Check(), "Docker is not available; skipping Aspire topology test.");

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.HappyPumi_AppHost>();
        appHost.Services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        appHost.Services.ConfigureHttpClientDefaults(client => client.AddStandardResilienceHandler());

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("api")
            .WaitAsync(StartupTimeout);

        // Hit the HTTPS endpoint: this exercises the self-signed dev cert (ADR-0007), which the test
        // process trusts via SSL_CERT_DIR (set by DevCertTrust at assembly load).
        using var http = app.CreateHttpClient("api", "https");
        // /api/user now requires the access token (ADR-0007); the CLI sends the same `token` scheme.
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", "dev");
        using var response = await http.GetAsync("/api/user");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
