using System;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.WorkflowAgent;

namespace HappyPumi.Api.Tests.Deployments;

/// <summary>
/// Unit tests for <see cref="GetWorkflowJobEndpoint.BuildJob"/> — the pure mapping from a queued deployment
/// to the runner job definition. The HTTP-level chain is covered in <see cref="WorkflowAgentTests"/>.
/// </summary>
public sealed class BuildJobTests
{
    private const string Backend = "http://10.0.0.1:5118";

    [Fact]
    public void WithoutTemplateRefEmitsDiagnosticVersionStep()
    {
        var row = new DeploymentRow { Operation = "preview" };

        var job = GetWorkflowJobEndpoint.BuildJob(row, Backend);

        // Step 0 is the placeholder the runner skips; the real step is last.
        Assert.Equal("prepare", job.Steps[0].Name);
        var step = job.Steps[^1];
        Assert.Equal("pulumi preview", step.Name);
        Assert.Equal("pulumi version", step.Run);
    }

    [Fact]
    public void WithTemplateRefEmitsFetchAndOperationSteps()
    {
        var row = new DeploymentRow
        {
            Org = "acme", Project = "web", Stack = "prod", Operation = "update",
            TemplateRef = "private/acme/web-template/2.1.0", JobToken = "jt-abc",
        };

        var job = GetWorkflowJobEndpoint.BuildJob(row, Backend);

        Assert.Equal(Backend, job.Env["PULUMI_BACKEND_URL"]);
        Assert.Equal("jt-abc", job.Env["PULUMI_ACCESS_TOKEN"]);
        // The runner skips step 0 and runs steps 1..N in one shared container, so fetch + extract + pulumi
        // all go in a single real step (the last one).
        Assert.Equal("prepare", job.Steps[0].Name);
        var run = job.Steps[^1].Run;
        Assert.Contains($"{Backend}/api/registry/templates/private/acme/web-template/versions/2.1.0/archive",
            run, StringComparison.Ordinal);
        Assert.Contains("tar -xzf", run, StringComparison.Ordinal);
        Assert.Contains("acme/web/prod", run, StringComparison.Ordinal);
        Assert.Contains("pulumi up --yes", run, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("preview", "pulumi preview")]
    [InlineData("destroy", "pulumi destroy --yes")]
    [InlineData("refresh", "pulumi refresh --yes")]
    public void TemplateOperationMapsToCliVerb(string operation, string expected)
    {
        var row = new DeploymentRow
        {
            Org = "o", Project = "p", Stack = "s", Operation = operation,
            TemplateRef = "private/o/t/1.0.0",
        };

        var job = GetWorkflowJobEndpoint.BuildJob(row, Backend);

        Assert.Contains(expected, job.Steps[^1].Run, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("succeeded", "succeeded")]
    [InlineData("SUCCESS", "succeeded")]
    [InlineData("completed", "succeeded")]
    [InlineData("failed", "failed")]
    [InlineData("failure", "failed")]
    [InlineData("error", "failed")]
    [InlineData("running", "running")]
    [InlineData("in-progress", "running")]
    public void NormalizeStatusMapsRunnerStrings(string input, string expected)
        => Assert.Equal(expected, UpdateStepStatusEndpoint.NormalizeStatus(input));

    [Fact]
    public void MalformedTemplateRefThrowsWithOffendingValue()
    {
        var row = new DeploymentRow { TemplateRef = "too/few/parts" };

        var ex = Assert.Throws<ArgumentException>(() => GetWorkflowJobEndpoint.BuildJob(row, Backend));
        Assert.Contains("too/few/parts", ex.Message, StringComparison.Ordinal);
    }

    // OWASP A03: a project/stack name carrying a shell quote-breakout must never reach the runner script.
    [Theory]
    [InlineData("p'; rm -rf / #")]
    [InlineData("p$(curl evil)")]
    [InlineData("p`whoami`")]
    [InlineData("p && reboot")]
    public void StackInjectionAttemptIsRejected(string malicious)
    {
        var row = new DeploymentRow
        {
            Org = "o", Project = "p", Stack = malicious, Operation = "update",
            TemplateRef = "private/o/t/1.0.0",
        };

        var ex = Assert.Throws<ArgumentException>(() => GetWorkflowJobEndpoint.BuildJob(row, Backend));
        Assert.Contains(malicious, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TemplateRefSegmentInjectionAttemptIsRejected()
    {
        // The name segment "t';wget evil" carries a quote + space → rejected before any script is built.
        var row = new DeploymentRow
        {
            Org = "o", Project = "p", Stack = "s", Operation = "update",
            TemplateRef = "private/o/t';wget evil/1.0.0",
        };
        Assert.Throws<ArgumentException>(() => GetWorkflowJobEndpoint.BuildJob(row, Backend));
    }
}
