using System.Diagnostics;

namespace HappyPumi.Api.Tests;

/// <summary>
/// Probes for a usable Docker daemon so container-backed tests (the Aspire topology test) can skip
/// rather than fail on hosts/CI without Docker. Result is cached for the test run.
/// </summary>
public static class DockerAvailable
{
    private static readonly Lazy<bool> Probe = new(RunDockerInfo);

    public static bool Check() => Probe.Value;

    private static bool RunDockerInfo()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (process is null)
                return false;

            if (!process.WaitForExit(TimeSpan.FromSeconds(10)))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // docker binary not on PATH, or not launchable.
            return false;
        }
    }
}
