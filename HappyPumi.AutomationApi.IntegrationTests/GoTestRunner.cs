using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HappyPumi.AutomationApi.IntegrationTests;

/// <summary>
/// Runs the Go auto-SDK test module as a child <c>go test -json</c> process against a live HappyPumi.
/// The Go SDK shells out to the pulumi binary, so the locally-built <c>.tools/bin</c> is prepended to
/// PATH and the self-signed dev cert is trusted via SSL_CERT_DIR (ADR-0007) — exactly as the CLI
/// wire-compat layer does. <c>-json</c> output is flattened into readable PASS/FAIL lines.
/// </summary>
public static class GoTestRunner
{
    public sealed record Result(int ExitCode, string Output);

    /// <summary>Runs the default <c>autoapi/</c> module.</summary>
    public static Task<Result> Run(
        string backendUrl, string token, string? certDir, string runFilter, CancellationToken ct)
        => RunModule("autoapi", backendUrl, token, certDir, extraEnv: null, runFilter, ct);

    /// <summary>
    /// Runs <c>go test</c> in a module directory relative to this project. <paramref name="extraEnv"/>
    /// is layered on top of the standard backend/token/cert env (used by the remote-workspace test to
    /// pass a container-reachable backend URL and git clone source).
    /// </summary>
    public static async Task<Result> RunModule(
        string moduleRelativeDir, string backendUrl, string token, string? certDir,
        IReadOnlyDictionary<string, string>? extraEnv, string runFilter, CancellationToken ct)
    {
        var moduleDir = Path.Combine(
            RepoPaths.RepoRoot, "HappyPumi.AutomationApi.IntegrationTests",
            moduleRelativeDir.Replace('/', Path.DirectorySeparatorChar));

        var psi = new ProcessStartInfo("go")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = moduleDir,
        };
        psi.ArgumentList.Add("test");
        psi.ArgumentList.Add("-json");
        psi.ArgumentList.Add("-count=1");
        psi.ArgumentList.Add("-timeout");
        psi.ArgumentList.Add("15m");
        if (!string.IsNullOrEmpty(runFilter))
        {
            psi.ArgumentList.Add("-run");
            psi.ArgumentList.Add(runFilter);
        }
        psi.ArgumentList.Add("./...");

        psi.Environment["PULUMI_BACKEND_URL"] = backendUrl;
        psi.Environment["PULUMI_ACCESS_TOKEN"] = token;
        psi.Environment["PULUMI_SKIP_UPDATE_CHECK"] = "true";
        // The locally-built CLI reports a dev version (v3.0.0-happypumi-dev) below the auto SDK's
        // client-side minimum-version gate, though it is built from the matching SDK source. Skip the
        // gate — the CLI is exercised as a black-box client (ADR-0008), and the wire behavior is what matters.
        psi.Environment["PULUMI_AUTOMATION_API_SKIP_VERSION_CHECK"] = "true";

        var binDir = Path.GetDirectoryName(RepoPaths.PulumiBinary ?? "");
        if (!string.IsNullOrEmpty(binDir))
            psi.Environment["PATH"] = binDir + Path.PathSeparator + (Environment.GetEnvironmentVariable("PATH") ?? "");
        if (!string.IsNullOrEmpty(certDir))
            psi.Environment["SSL_CERT_DIR"] = certDir;
        if (extraEnv is not null)
            foreach (var (k, v) in extraEnv)
                psi.Environment[k] = v;

        using var process = new Process { StartInfo = psi };
        var raw = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (raw) raw.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (raw) raw.AppendLine(e.Data); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct);

        return new Result(process.ExitCode, Summarize(raw.ToString()));
    }

    /// <summary>Flattens <c>go test -json</c> action events into readable per-test lines plus raw output.</summary>
    private static string Summarize(string jsonLines)
    {
        var sb = new StringBuilder();
        foreach (var line in jsonLines.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith('{')) { sb.AppendLine(line); continue; }
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                var action = root.TryGetProperty("Action", out var a) ? a.GetString() : null;
                if (action is "pass" or "fail" or "skip" && root.TryGetProperty("Test", out var t))
                    sb.AppendLine($"{action!.ToUpperInvariant()} {t.GetString()}");
                else if (action == "output" && root.TryGetProperty("Output", out var o))
                    sb.Append(o.GetString());
            }
            catch (JsonException) { sb.AppendLine(line); }
        }
        return sb.ToString();
    }
}
