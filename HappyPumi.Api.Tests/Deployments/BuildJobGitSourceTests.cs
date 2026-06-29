using System;
using System.Linq;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.WorkflowAgent;

namespace HappyPumi.Api.Tests.Deployments;

/// <summary>
/// Unit tests for the runner job builder's git-source mode (the remote-workspace path). BuildJob is
/// internal; visible to this assembly via InternalsVisibleTo. No infra needed.
/// </summary>
public sealed class BuildJobGitSourceTests
{
    private const string Backend = "https://hp.test";

    private static DeploymentRow GitRow(string operation = "update", string? branch = "main", string? dir = "app") => new()
    {
        Id = "d1", Org = "organization", Project = "infra", Stack = "prod", Operation = operation,
        GitRepoUrl = "https://example.test/r.git", GitBranch = branch, GitRepoDir = dir,
    };

    private static string Script(DeploymentRow row)
        => string.Join('\n', GetWorkflowJobEndpoint.BuildJob(row, Backend).Steps.Select(s => s.Run));

    [Fact]
    public void GitSourceEmitsCloneEnterDirSelectAndOperation()
    {
        var script = Script(GitRow());

        Assert.Contains("git clone", script);
        Assert.Contains("https://example.test/r.git", script);
        Assert.Contains("-b 'main'", script);
        Assert.Contains("cd repo", script);
        Assert.Contains("cd 'app'", script);
        Assert.Contains("pulumi stack select --create 'organization/infra/prod'", script);
        // operation "update" maps to the CLI verb `pulumi up`.
        Assert.Contains("pulumi up --yes", script);
    }

    [Fact]
    public void GitSourceWithoutBranchOrDirOmitsThoseLines()
    {
        var script = Script(GitRow(branch: null, dir: null));

        Assert.DoesNotContain("-b '", script);
        Assert.Contains("git clone 'https://example.test/r.git' repo", script);
        Assert.Contains("cd repo", script);
        // No project-subdir cd beyond entering the cloned repo.
        Assert.DoesNotContain("cd 'app'", script);
    }

    [Fact]
    public void GitSourcePreviewOperationRunsPulumiPreview()
    {
        var script = Script(GitRow(operation: "preview"));

        Assert.Contains("pulumi preview", script);
        Assert.DoesNotContain("--yes", script);
    }

    [Fact]
    public void GitSourceTakesPrecedenceOverTemplateRef()
    {
        var row = GitRow();
        row.TemplateRef = "private/acme/widget/1.0.0";

        var script = Script(row);

        Assert.Contains("git clone", script);
        Assert.DoesNotContain("registry/templates", script); // template path not taken
    }

    [Fact]
    public void GitProtocolUrlIsAccepted()
    {
        var row = GitRow(branch: "master", dir: null);
        row.GitRepoUrl = "git://172.17.0.1:9418/empty-stack.git";

        var script = Script(row);

        Assert.Contains("git clone -b 'master' 'git://172.17.0.1:9418/empty-stack.git' repo", script);
    }

    [Fact]
    public void GitUrlWithShellMetacharactersIsRejected()
    {
        var row = GitRow();
        row.GitRepoUrl = "https://x/$(rm -rf /).git"; // contains whitespace -> fails the http(s) URL allow-list

        Assert.Throws<ArgumentException>(() => GetWorkflowJobEndpoint.BuildJob(row, Backend));
    }

    [Fact]
    public void GitBranchWithShellMetacharactersIsRejected()
    {
        var row = GitRow(branch: "main; rm -rf /");

        Assert.Throws<ArgumentException>(() => GetWorkflowJobEndpoint.BuildJob(row, Backend));
    }
}
