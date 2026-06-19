using System.Collections.Generic;
using HappyPumi.Api.Endpoints.Environments;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Unit tests for the non-provider ESC built-in functions.</summary>
public sealed class EscBuiltinsTests
{
    [Fact]
    public void JoinConcatenatesPartsWithSeparator()
    {
        var arg = new List<object?> { ", ", new List<object?> { "a", "b", "c" } };
        Assert.Equal("a, b, c", EscBuiltins.Apply("fn::join", arg));
    }

    [Fact]
    public void Base64RoundTrips()
    {
        Assert.Equal("aGVsbG8=", EscBuiltins.Apply("fn::toBase64", "hello"));
        Assert.Equal("hello", EscBuiltins.Apply("fn::fromBase64", "aGVsbG8="));
    }

    [Fact]
    public void JsonRoundTrips()
    {
        Assert.Equal("{\"x\":1}", EscBuiltins.Apply("fn::toJSON", new Dictionary<string, object?> { ["x"] = 1L }));
        var parsed = (Dictionary<string, object?>)EscBuiltins.Apply("fn::fromJSON", "{\"y\":2}")!;
        Assert.Equal(2L, parsed["y"]);
    }

    [Fact]
    public void ToStringStringifiesScalarsAndMaps()
    {
        Assert.Equal("42", EscBuiltins.Apply("fn::toString", 42L));
        Assert.Equal("true", EscBuiltins.Apply("fn::toString", true));
        Assert.Equal("{\"a\":\"b\"}", EscBuiltins.Apply("fn::toString", new Dictionary<string, object?> { ["a"] = "b" }));
    }

    [Fact]
    public void MalformedJoinThrowsWithOffendingValue()
    {
        var ex = Assert.Throws<System.ArgumentException>(() => EscBuiltins.Apply("fn::join", "not-a-pair"));
        Assert.Contains("fn::join", ex.Message);
    }

    [Fact]
    public void EvaluatorAppliesBuiltinsAndMarksOpenUnknown()
    {
        const string yaml = """
        values:
          name:
            fn::join: [".", ["app", "prod"]]
          secret:
            fn::open::azure-keyvault:
              vault: v
        """;

        var props = EnvironmentEvaluator.Evaluate(yaml);

        Assert.Equal("app.prod", props["name"].Value);
        Assert.True(props["secret"].Unknown); // a live provider value is unknown without opening
    }
}
