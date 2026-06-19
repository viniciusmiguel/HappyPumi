using System.Collections.Generic;
using HappyPumi.Api.Endpoints.Environments;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Unit tests for the evaluator's fn::rotate handling (opens to state.current).</summary>
public sealed class EscRotateEvaluatorTests
{
    [Fact]
    public void RotatedSecretResolvesToCurrentStateAndIsSecret()
    {
        const string yaml = """
        values:
          creds:
            fn::rotate::aws-iam:
              inputs: { region: us-east-1, user: bot }
              state:
                current: { accessKeyId: AKIA, secretAccessKey: shh }
        """;

        var props = EnvironmentEvaluator.Evaluate(yaml);

        Assert.True(props["creds"].Secret);
        var current = (Dictionary<string, object?>)props["creds"].Value!;
        Assert.Equal("AKIA", current["accessKeyId"]);
    }

    [Fact]
    public void NotYetRotatedIsUnknown()
    {
        const string yaml = """
        values:
          creds:
            fn::rotate::aws-iam:
              inputs: { region: us-east-1, user: bot }
        """;

        var props = EnvironmentEvaluator.Evaluate(yaml);

        Assert.True(props["creds"].Unknown);
    }
}
