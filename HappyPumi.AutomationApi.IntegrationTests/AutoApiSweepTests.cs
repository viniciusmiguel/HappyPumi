using Xunit.Abstractions;

namespace HappyPumi.AutomationApi.IntegrationTests;

/// <summary>
/// The Automation API feature sweep: each [Fact] runs a group of Go auto-SDK subtests against the
/// shared HappyPumi server. Grouping keeps failures isolated to a meaningful area while reusing one
/// server boot for the whole collection. The Go subtests live in autoapi/.
/// </summary>
[Collection(HappyPumiServerCollection.Name)]
public sealed class AutoApiSweepTests(HappyPumiServer server, ITestOutputHelper output)
{
    private const string Token = "happypumi-auto-token";

    private async Task RunGroup(string goTestFilter)
    {
        var result = await GoTestRunner.Run(
            server.BaseUrl, Token, TestSupport.DevCertTrust.CertDir, goTestFilter, default);
        output.WriteLine(result.Output);
        Assert.True(result.ExitCode == 0, $"go test ({goTestFilter}) failed:\n{result.Output}\n{server.ServerLog()}");
    }

    [Fact]
    public Task StackManagement() => RunGroup("TestOrgDefault|TestStackLifecycle");

    [Fact]
    public Task UpdateLifecycle() => RunGroup("TestInlineLifecycle");

    [Fact]
    public Task TagsRenameAndState() => RunGroup("TestTagRoundTrip|TestRename|TestExportImport");
}
