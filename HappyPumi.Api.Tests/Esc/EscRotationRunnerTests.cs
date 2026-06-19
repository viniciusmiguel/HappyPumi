using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Endpoints.Environments;
using HappyPumi.Api.Esc;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for executing secret rotation (fake store + fake rotator).</summary>
public sealed class EscRotationRunnerTests
{
    private static readonly EnvCoordinates Coords = new("happypumi", "proj", "app");

    private const string RotatableYaml = """
    values:
      creds:
        fn::rotate::test-rotator:
          inputs: { region: us-east-1 }
          state:
            current: { password: old-pw }
    """;

    private static EscRotationRunner Build(IEnvironmentStore store, FakeEscRotator rotator)
        => new(store, new EscRotatorRegistry(new IEscRotator[] { rotator }), new EscRotationHistory());

    [Fact]
    public async Task RotatesWritesNewStateAndBumpsRevision()
    {
        var store = new FakeEnvironmentStore().With(Coords, RotatableYaml);
        var rotator = new FakeEscRotator();
        var runner = Build(store, rotator);

        var ev = await runner.RotateAsync(Coords, "tester", CancellationToken.None);

        Assert.Equal("succeeded", ev!.Status);
        Assert.Equal("creds", ev.Rotations.Single().EnvironmentPath);
        Assert.True(ev.PostRotationRevision > ev.PreRotationRevision); // a new revision was written
        Assert.Equal("old-pw", rotator.ReceivedCurrent!["password"]); // previous current handed to the rotator

        // The stored definition now opens to the rotated value.
        var props = EnvironmentEvaluator.Evaluate(store.Get(Coords)!.Yaml);
        Assert.True(props["creds"].Secret);
        Assert.Equal("new-pw", ((Dictionary<string, object?>)props["creds"].Value!)["password"]);
    }

    [Fact]
    public async Task NoRotateDeclarationsIsANoOp()
    {
        var store = new FakeEnvironmentStore().With(Coords, "values:\n  plain: hello\n");
        var runner = Build(store, new FakeEscRotator());

        var ev = await runner.RotateAsync(Coords, "tester", CancellationToken.None);

        Assert.Empty(ev!.Rotations);
        Assert.Equal(ev.PreRotationRevision, ev.PostRotationRevision); // no new revision
    }

    [Fact]
    public async Task MissingEnvironmentReturnsNull()
    {
        var runner = Build(new FakeEnvironmentStore(), new FakeEscRotator());
        Assert.Null(await runner.RotateAsync(Coords, "tester", CancellationToken.None));
    }
}
