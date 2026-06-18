using System.Runtime.InteropServices;

namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Resolves on-disk locations the CLI integration harness needs: the repo root (the directory
/// containing <c>HappyPumi.slnx</c>), the built API entry DLL, and the <c>pulumi</c> binary.
/// Paths are derived from the running test assembly so the harness works from any CWD.
/// </summary>
public static class RepoPaths
{
    private const string SolutionFile = "HappyPumi.slnx";

    /// <summary>The directory containing HappyPumi.slnx. Throws if not found walking up from the test bin.</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary>Build configuration the tests were compiled in (Debug/Release), reused for the API DLL path.</summary>
    public static string Configuration { get; } =
        AppContext.BaseDirectory.Replace('\\', '/').Contains("/Release/", StringComparison.Ordinal) ? "Release" : "Debug";

    /// <summary>Path to the built API entry DLL we launch as the server under test.</summary>
    public static string ApiDll { get; } = Path.Combine(
        RepoRoot, "HappyPumi.Api", "bin", Configuration, "net10.0", "HappyPumi.Api.dll");

    /// <summary>
    /// The pulumi CLI to drive. Resolution order: PULUMI_BIN env, then the repo-local build
    /// (.tools/bin/pulumi from tools/build-pulumi-cli.sh), then PATH. Null if none is found.
    /// </summary>
    public static string? PulumiBinary { get; } = FindPulumi();

    /// <summary>Absolute path to a CLI test fixture (a Pulumi project) under this project's fixtures/ dir.</summary>
    public static string Fixture(string name) =>
        Path.Combine(RepoRoot, "HappyPumi.Cli.IntegrationTests", "fixtures", name);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFile)))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate '{SolutionFile}' walking up from '{AppContext.BaseDirectory}'.");
    }

    private static string? FindPulumi()
    {
        var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pulumi.exe" : "pulumi";

        var fromEnv = Environment.GetEnvironmentVariable("PULUMI_BIN");
        if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
            return fromEnv;

        var local = Path.Combine(RepoRoot, ".tools", "bin", exe);
        if (File.Exists(local))
            return local;

        foreach (var path in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            var candidate = Path.Combine(path, exe);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
