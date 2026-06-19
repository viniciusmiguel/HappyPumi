namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Wire-compatibility for the <c>pulumi env</c> (ESC) command family against a real HappyPumi + Postgres.
/// Drives the actual binary's environment subcommands the same way the stack/config tests drive theirs.
/// Commands whose backend is implemented get real round-trip assertions; those needing not-yet-built pieces
/// are <c>Skip</c>ped naming the gap (see the ESC parity audit).
/// </summary>
public sealed class EnvironmentCliTests(HappyPumiServer server) : CliTestBase(server)
{
    // esc environment names are <project>/<env>; the CLI prefixes the user's default org.
    private static string UniqueEnv(string label) => $"hp-esc-{label}-{Guid.NewGuid():N}/dev";

    [Fact]
    public async Task EnvInitLsRm()
    {
        using var cli = LoggedIn();
        var env = UniqueEnv("life");

        (await Run(cli, "env", "init", env)).EnsureSucceeded();   // CreateEnvironment
        (await Run(cli, "env", "ls")).EnsureSucceeded();          // ListEnvironments
        (await Run(cli, "env", "rm", "--yes", env)).EnsureSucceeded(); // DeleteEnvironment
    }

    [Fact]
    public async Task EnvSetThenOpenResolvesValue()
    {
        using var cli = LoggedIn();
        var env = UniqueEnv("setopen");
        (await Run(cli, "env", "init", env)).EnsureSucceeded();

        // set: read (GetEnvironment + revision header) -> modify -> UpdateEnvironment.
        (await Run(cli, "env", "set", env, "myapp.region", "eu-west-1", "--plaintext")).EnsureSucceeded();

        // open: evaluate the definition and read the resolved session back.
        var open = await Run(cli, "env", "open", env);
        open.EnsureSucceeded();
        Assert.Contains("eu-west-1", open.StdOut, StringComparison.Ordinal);

        (await Run(cli, "env", "rm", "--yes", env)).EnsureSucceeded();
    }

    [Fact(Skip = "env get <path> needs the `exprs` AST in the CheckYAML response (esc env_get getEnvExpr); not yet built")]
    public Task EnvGetByPath() => Task.CompletedTask;
}
