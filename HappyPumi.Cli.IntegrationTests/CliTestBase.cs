namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Shared harness for CLI integration tests: hands out a pulumi binary already logged in to the
/// running HappyPumi server, and runs commands inside the resourceless <c>empty-stack</c> fixture
/// (so project-scoped commands have a Pulumi.yaml to resolve). One server is shared per collection.
/// </summary>
[Collection(HappyPumiServerCollection.Name)]
public abstract class CliTestBase(HappyPumiServer server)
{
    /// <summary>Base URL of the running server under test (e.g. https://localhost:5123).</summary>
    protected string ServerBaseUrl => server.BaseUrl;

    /// <summary>The resourceless project fixture used as CWD for project/stack-scoped commands.</summary>
    protected static string Fixture => RepoPaths.Fixture("empty-stack");

    /// <summary>A fresh isolated CLI; not yet logged in. Caller disposes.</summary>
    protected static PulumiCli NewCli() =>
        new(RepoPaths.PulumiBinary ?? throw new InvalidOperationException(
            "pulumi binary not found. Build it with `make pulumi` (tools/build-pulumi-cli.sh) or set PULUMI_BIN."));

    /// <summary>A fresh isolated CLI already logged in to the server under test. Caller disposes.</summary>
    protected PulumiCli LoggedIn()
    {
        var cli = NewCli();
        cli.RunAsync(CancellationToken.None, "login", server.BaseUrl).GetAwaiter().GetResult().EnsureSucceeded();
        return cli;
    }

    /// <summary>Runs a pulumi command inside the fixture project directory.</summary>
    protected static async Task<CliResult> Run(PulumiCli cli, params string[] args)
        => await RunIn(cli, Fixture, args);

    /// <summary>Runs a pulumi command inside an explicit working directory.</summary>
    protected static async Task<CliResult> RunIn(PulumiCli cli, string cwd, params string[] args)
        => await cli.RunAsync(CancellationToken.None, new[] { "--cwd", cwd }.Concat(args).ToArray());

    /// <summary>A unique fully-qualified stack name under the default org + fixture project.</summary>
    protected static string UniqueStack(string label) =>
        $"organization/happypumi-empty-stack/{label}-{Guid.NewGuid():N}";

    /// <summary>
    /// Creates a throwaway project directory containing only a minimal Pulumi.yaml. Enough for
    /// config/stack-management commands (which never build the program), and isolated per test so
    /// parallel runs don't fight over Pulumi.&lt;stack&gt;.yaml files. Caller should not need to clean up
    /// (temp dir), but stacks it creates in the backend should be removed via `stack rm`.
    /// </summary>
    protected static string NewTempProject(out string projectName)
    {
        projectName = "hp-cli-" + Guid.NewGuid().ToString("N")[..8];
        var dir = Path.Combine(Path.GetTempPath(), projectName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(
            Path.Combine(dir, "Pulumi.yaml"),
            $"name: {projectName}\nruntime: go\ndescription: throwaway CLI integration project\n");
        return dir;
    }
}
