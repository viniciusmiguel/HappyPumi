using System.Diagnostics;
using System.Text.Json;
using Xunit.Abstractions;

namespace HappyPumi.AutomationApi.IntegrationTests;

/// <summary>
/// Docker-backed end-to-end test for HappyPumi's remote-workspace (git source) deployment path. Brings up
/// the deployment-agent compose topology (Postgres + HappyPumi + the prebuilt agent + a git server), then
/// triggers a git-source deployment and asserts the prebuilt agent clones the repo and runs `pulumi up`
/// against HappyPumi to success. Auto-skips when Docker is unavailable, matching the Aspire topology test.
///
/// Why the real CLI's <c>pulumi deployment run</c> and not the Go auto SDK's <c>RemoteStack.Up</c>: both
/// POST the *same* git-source wire contract (CreateAPIDeploymentHandlerV2) and poll the same status — that
/// contract is what this validates. The auto SDK's RemoteStack.Up shells out to <c>pulumi up --remote</c>,
/// which in the pinned CLI (v3.246) requires a local Pulumi.yaml in its temp workspace (the project read
/// happens before the --remote branch in up.go); `deployment run` is the project-tolerant remote trigger.
/// </summary>
public sealed class RemoteWorkspaceTests(ITestOutputHelper output)
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromMinutes(6);
    private static readonly TimeSpan DeploymentTimeout = TimeSpan.FromMinutes(6);

    // happypumi is published on host :5118 over HTTP; the executor reaches it at 172.17.0.1:5118 and clones
    // the program from the git server, both on the host network (see the compose topology notes).
    private const string HostBackend = "http://localhost:5118";
    private const string GitUrl = "git://172.17.0.1:9418/empty-stack.git";
    private const string Org = "organization";
    private const string Project = "happypumi-empty-stack";
    private const string Stack = "remote1";

    private static string ComposeFile =>
        Path.Combine(RepoPaths.RepoRoot, "deploy", "deployment-agent", "docker-compose.yml");

    [SkippableFact]
    public async Task Remote_git_source_deployment_succeeds()
    {
        Skip.IfNot(await DockerAvailable(), "Docker not available");
        Skip.If(RepoPaths.PulumiBinary is null, "pulumi binary not built (make pulumi)");

        // Fresh DB each run so the stack/deployment names are clean.
        Assert.Equal(0, await Compose("down -v"));
        Assert.Equal(0, await Compose("up --build -d"));
        try
        {
            await WaitUntilReady();
            await CreateStack();

            var deploymentId = await RunDeployment();
            output.WriteLine($"deployment id: {deploymentId}");

            var status = await PollUntilComplete(deploymentId);
            Assert.Equal("succeeded", status);
        }
        finally
        {
            await Compose("logs --no-color --tail 60 agent", capture: output);
            await Compose("down -v");
        }
    }

    /// <summary>Creates the target stack via the API (the remote trigger requires it to pre-exist).</summary>
    private async Task CreateStack()
    {
        using var client = new HttpClient { BaseAddress = new Uri(HostBackend) };
        using var resp = await client.PostAsync(
            $"/api/stacks/{Org}/{Project}",
            new StringContent($"{{\"stackName\":\"{Stack}\"}}", System.Text.Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>Triggers a git-source deployment via the real CLI and returns the new deployment id.</summary>
    private async Task<string> RunDeployment()
    {
        var (code, stdout, stderr) = await RunPulumi(
            "deployment", "run", "update", GitUrl,
            "--git-branch=refs/heads/master",
            "--stack", $"{Org}/{Project}/{Stack}");
        output.WriteLine(stdout);
        output.WriteLine(stderr);
        Assert.True(code == 0, $"`pulumi deployment run` failed ({code}):\n{stderr}");

        // Output line: "View Live: /api/stacks/.../deployments/<id>"
        var marker = "/deployments/";
        var idx = stdout.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(idx >= 0, $"could not find deployment id in output:\n{stdout}");
        var id = new string(stdout[(idx + marker.Length)..].TakeWhile(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        Assert.False(string.IsNullOrEmpty(id), "parsed empty deployment id");
        return id;
    }

    /// <summary>Polls the deployment until it completes; returns the terminal status.</summary>
    private async Task<string> PollUntilComplete(string deploymentId)
    {
        using var client = new HttpClient { BaseAddress = new Uri(HostBackend) };
        client.DefaultRequestHeaders.Add("Authorization", "token t");
        using var cts = new CancellationTokenSource(DeploymentTimeout);
        while (!cts.IsCancellationRequested)
        {
            using var resp = await client.PostAsync($"/api/agent-workflows/deployment:{deploymentId}/check", null, cts.Token);
            var body = await resp.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("complete", out var c) && c.GetBoolean())
                return root.GetProperty("status").GetString() ?? "";
            await Task.Delay(5000, cts.Token);
        }
        throw new TimeoutException($"deployment {deploymentId} did not complete within {DeploymentTimeout}.");
    }

    private static async Task<bool> DockerAvailable()
    {
        try { return (await RunProcess("docker", new[] { "version" }, null)).Code == 0; }
        catch (Exception) { return false; }
    }

    private async Task WaitUntilReady()
    {
        using var client = new HttpClient { BaseAddress = new Uri(HostBackend) };
        using var cts = new CancellationTokenSource(ReadyTimeout);
        while (!cts.IsCancellationRequested)
        {
            try
            {
                using var resp = await client.GetAsync("/api/capabilities", cts.Token);
                if (resp.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { /* not up yet */ }
            await Task.Delay(2000, cts.Token);
        }
        throw new TimeoutException($"HappyPumi (compose) did not become ready at {HostBackend} within {ReadyTimeout}.");
    }

    private Task<int> Compose(string args, ITestOutputHelper? capture = null)
    {
        var argv = new List<string> { "compose", "-f", ComposeFile };
        argv.AddRange(args.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return RunProcess("docker", argv, capture).ContinueWith(t => t.Result.Code);
    }

    private static async Task<(int Code, string StdOut, string StdErr)> RunPulumi(params string[] args)
    {
        var pulumi = RepoPaths.PulumiBinary!;
        var binDir = Path.GetDirectoryName(pulumi)!;
        var env = new Dictionary<string, string>
        {
            ["PATH"] = binDir + Path.PathSeparator + (Environment.GetEnvironmentVariable("PATH") ?? ""),
            ["PULUMI_BACKEND_URL"] = HostBackend,
            ["PULUMI_ACCESS_TOKEN"] = "t",
            ["PULUMI_EXPERIMENTAL"] = "true",
            ["PULUMI_SKIP_UPDATE_CHECK"] = "true",
            ["HOME"] = Path.Combine(Path.GetTempPath(), "hp-remote-home"),
        };
        Directory.CreateDirectory(env["HOME"]);
        var r = await RunProcess(pulumi, args, null, env);
        return (r.Code, r.StdOut, r.StdErr);
    }

    private static async Task<(int Code, string StdOut, string StdErr)> RunProcess(
        string file, IEnumerable<string> args, ITestOutputHelper? capture, IReadOnlyDictionary<string, string>? env = null)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        if (env is not null)
            foreach (var (k, v) in env) psi.Environment[k] = v;

        using var p = new Process { StartInfo = psi };
        var outSb = new System.Text.StringBuilder();
        var errSb = new System.Text.StringBuilder();
        p.OutputDataReceived += (_, e) => { if (e.Data is not null) { lock (outSb) outSb.AppendLine(e.Data); capture?.WriteLine(e.Data); } };
        p.ErrorDataReceived += (_, e) => { if (e.Data is not null) { lock (errSb) errSb.AppendLine(e.Data); capture?.WriteLine(e.Data); } };
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        await p.WaitForExitAsync();
        return (p.ExitCode, outSb.ToString(), errSb.ToString());
    }
}
