using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Environments;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Unit tests for the ESC environment evaluator (interpolation + secrets).</summary>
public sealed class EnvironmentEvaluatorTests
{
    private static EscValue Child(EscValue parent, string key)
        => ((Dictionary<string, EscValue>)parent.Value!)[key];

    [Fact]
    public void ResolvesDottedInterpolation()
    {
        const string yaml = """
        values:
          aws:
            region: us-west-2
          env:
            region: ${aws.region}
            url: https://${aws.region}.example.com
        """;

        var props = EnvironmentEvaluator.Evaluate(yaml);

        var env = props["env"];
        Assert.Equal("us-west-2", Child(env, "region").Value);
        Assert.Equal("https://us-west-2.example.com", Child(env, "url").Value);
    }

    [Fact]
    public void FlagsSecrets()
    {
        const string yaml = """
        values:
          db:
            password:
              fn::secret: hunter2
        """;

        var props = EnvironmentEvaluator.Evaluate(yaml);

        var password = Child(props["db"], "password");
        Assert.True(password.Secret);
    }

    [Fact]
    public void EmptyOrMissingValuesYieldsEmptyTree()
    {
        Assert.Empty(EnvironmentEvaluator.Evaluate(""));
        Assert.Empty(EnvironmentEvaluator.Evaluate("imports: []\n"));
    }

    [Fact]
    public void UnresolvedReferenceBecomesNull()
    {
        var props = EnvironmentEvaluator.Evaluate("values:\n  x: ${does.not.exist}\n");
        Assert.Null(props["x"].Value);
    }
}
