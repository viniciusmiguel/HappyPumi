using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Testcontainers.PostgreSql;

namespace HappyPumi.Cli.IntegrationTests;

/// <summary>
/// Runs the built HappyPumi API as a real out-of-process HTTP server on a free loopback port, so the
/// actual pulumi CLI can talk to it over the wire (a TestServer would only work in-process). Shared
/// across the test collection via <see cref="HappyPumiServerCollection"/>.
/// </summary>
public sealed class HappyPumiServer : IAsyncLifetime
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);

    private Process? _process;
    private readonly StringBuilder _serverLog = new();
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:16-alpine").Build();

    /// <summary>Base URL the CLI logs in against, e.g. http://127.0.0.1:5123.</summary>
    public string BaseUrl { get; private set; } = "";

    public async Task InitializeAsync()
    {
        if (!File.Exists(RepoPaths.ApiDll))
            throw new InvalidOperationException(
                $"API not built at '{RepoPaths.ApiDll}'. Build the solution first (e.g. `make build` or `dotnet build HappyPumi.slnx`).");

        // All state persists to Postgres (ADR-0005); the subprocess applies the EF migration on startup.
        await _db.StartAsync();

        var port = GetFreeLoopbackPort();
        // HTTPS with the self-signed dev cert (ADR-0007). Host is "localhost" to match the cert SAN;
        // Development env makes Kestrel pick up the dev cert automatically.
        BaseUrl = $"https://localhost:{port}";

        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(RepoPaths.ApiDll)!,
        };
        psi.ArgumentList.Add(RepoPaths.ApiDll);
        psi.Environment["ASPNETCORE_URLS"] = BaseUrl;
        psi.Environment["DOTNET_ENVIRONMENT"] = "Development";
        psi.Environment["ConnectionStrings__happypumidb"] = _db.GetConnectionString();

        _process = new Process { StartInfo = psi };
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (_serverLog) _serverLog.AppendLine(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (_serverLog) _serverLog.AppendLine(e.Data); };
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        await WaitUntilReadyAsync();
    }

    /// <summary>Polls GET /api/user (the CLI's own login probe) until the server answers or we time out.</summary>
    private async Task WaitUntilReadyAsync()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        using var cts = new CancellationTokenSource(ReadyTimeout);

        while (!cts.IsCancellationRequested)
        {
            if (_process!.HasExited)
                throw new InvalidOperationException(
                    $"HappyPumi API exited early (code {_process.ExitCode}) before becoming ready.\n{ServerLog()}");

            try
            {
                // Probe an anonymous endpoint: /api/user now requires the access token (ADR-0007), so a
                // tokenless readiness check would always see 401. /api/capabilities stays anonymous.
                using var response = await client.GetAsync("/api/capabilities", cts.Token);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException) { /* not listening yet */ }

            await Task.Delay(200, cts.Token);
        }

        throw new TimeoutException($"HappyPumi API did not become ready within {ReadyTimeout}.\n{ServerLog()}");
    }

    public string ServerLog()
    {
        lock (_serverLog)
            return $"--- server log ---\n{_serverLog}";
    }

    private static int GetFreeLoopbackPort()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        try { return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port; }
        finally { listener.Stop(); }
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
        }
        catch (InvalidOperationException) { /* already gone */ }
        finally
        {
            _process?.Dispose();
            await _db.DisposeAsync();
        }
    }
}

/// <summary>One server process shared across all CLI integration tests in the collection.</summary>
[CollectionDefinition(Name)]
public sealed class HappyPumiServerCollection : ICollectionFixture<HappyPumiServer>
{
    public const string Name = "happypumi-cli";
}
