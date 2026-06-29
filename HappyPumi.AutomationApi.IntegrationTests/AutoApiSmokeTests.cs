using Xunit.Abstractions;

namespace HappyPumi.AutomationApi.IntegrationTests;

/// <summary>
/// Drives the Go Automation API SDK against a live HappyPumi. Each [Fact] boots (shares) the
/// out-of-process HTTPS server and runs a named Go test via <see cref="GoTestRunner"/>. The server
/// fixture is the same one the CLI wire-compat layer uses (reused by file link).
/// </summary>
[Collection(HappyPumiServerCollection.Name)]
public sealed class AutoApiSmokeTests(HappyPumiServer server, ITestOutputHelper output)
{
    // HappyPumi's dev auth accepts any non-empty access token (PulumiTokenAuthHandler).
    private const string Token = "happypumi-auto-token";

    [Fact]
    public async Task WhoAmI_against_happypumi_succeeds()
    {
        var result = await GoTestRunner.Run(
            server.BaseUrl, Token, TestSupport.DevCertTrust.CertDir, "TestWhoAmI", default);
        output.WriteLine(result.Output);
        Assert.True(result.ExitCode == 0, $"go test failed:\n{result.Output}\n{server.ServerLog()}");
    }
}
