using System.Diagnostics;
using System.Text;

namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Thin wrapper around the real <c>pulumi</c> binary (per CLAUDE.md: wrap third-party tools behind
/// an owned interface). Each instance is pinned to an isolated PULUMI_HOME and a fixed
/// PULUMI_ACCESS_TOKEN so tests never touch the developer's real ~/.pulumi or credentials.
///
/// Clean-room note: the CLI is used purely as a black-box HTTP client over the wire
/// (see docs/adr/0008-clean-room-implementation.md).
/// </summary>
public sealed class PulumiCli : IDisposable
{
    private readonly string _binary;
    private readonly string _pulumiHome;

    public PulumiCli(string binary)
    {
        _binary = binary;
        // Isolated home per CLI instance — wiped on Dispose.
        _pulumiHome = Path.Combine(Path.GetTempPath(), "happypumi-pulumi-home-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_pulumiHome);
    }

    /// <summary>Runs <c>pulumi {args}</c> and captures the result. Never throws on a non-zero exit.</summary>
    public async Task<CliResult> RunAsync(CancellationToken ct, params string[] args)
    {
        var psi = new ProcessStartInfo(_binary)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = _pulumiHome,
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        // Isolate state and skip interactive/telemetry behaviour that would hang a test run.
        psi.Environment["PULUMI_HOME"] = _pulumiHome;
        psi.Environment["PULUMI_ACCESS_TOKEN"] = "happypumi-integration-token";
        psi.Environment["PULUMI_SKIP_UPDATE_CHECK"] = "true";
        psi.Environment["PULUMI_SKIP_CONFIRMATIONS"] = "true";

        // Put the CLI's own directory first on PATH so the engine resolves the co-located
        // language host (pulumi-language-go) locally instead of fetching a plugin — see plugins.go.
        var binDir = Path.GetDirectoryName(_binary);
        if (!string.IsNullOrEmpty(binDir))
            psi.Environment["PATH"] = binDir + Path.PathSeparator + (Environment.GetEnvironmentVariable("PATH") ?? "");

        // Trust the self-signed dev cert: Go honors SSL_CERT_DIR just like OpenSSL, so the CLI accepts
        // HappyPumi's HTTPS endpoint (ADR-0007). Set by DevCertTrust at test-assembly load.
        var certDir = TestSupport.DevCertTrust.CertDir;
        if (!string.IsNullOrEmpty(certDir))
            psi.Environment["SSL_CERT_DIR"] = certDir;

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct);

        return new CliResult(
            $"pulumi {string.Join(' ', args)}", process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    public void Dispose()
    {
        try { Directory.Delete(_pulumiHome, recursive: true); }
        catch (DirectoryNotFoundException) { /* already gone */ }
        catch (IOException) { /* best-effort temp cleanup */ }
    }
}
