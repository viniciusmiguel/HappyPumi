namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Tier 0 (login) wire-compatibility: drives the real pulumi CLI against a running HappyPumi and
/// asserts the CLI accepts our responses. Needs no Pulumi program and no infra at all.
/// </summary>
public sealed class LoginTests(HappyPumiServer server) : CliTestBase(server)
{
    [Fact]
    public async Task LoginSucceeds()
    {
        using var cli = NewCli();

        var result = await cli.RunAsync(CancellationToken.None, "login", ServerBaseUrl);

        result.EnsureSucceeded();
    }

    [Fact]
    public async Task WhoAmIReportsTheUserFromApiUser()
    {
        using var cli = NewCli();
        (await cli.RunAsync(CancellationToken.None, "login", ServerBaseUrl)).EnsureSucceeded();

        var who = await cli.RunAsync(CancellationToken.None, "whoami");

        who.EnsureSucceeded();
        // githubLogin returned by GET /api/user (see GetCurrentUserEndpoint).
        Assert.Contains("happypumi", who.StdOut, StringComparison.OrdinalIgnoreCase);
    }
}
