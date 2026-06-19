using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.PulumiStacks;
using HappyPumi.Api.State;
using Microsoft.Extensions.DependencyInjection;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the fn::open::pulumi-stacks provider (against a fake stack-outputs source).</summary>
public sealed class PulumiStacksProviderTests
{
    private const string Org = "happypumi";

    private static PulumiStacksProvider Build(IStackOutputsSource source)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => source); // provider resolves it inside a scope it creates
        var sp = services.BuildServiceProvider();
        return new PulumiStacksProvider(sp.GetRequiredService<IServiceScopeFactory>());
    }

    private static Dictionary<string, object?> Inputs(string outputKey, string project, string stack)
        => new()
        {
            ["organization"] = Org,
            ["stacks"] = new Dictionary<string, object?>
            {
                [outputKey] = new Dictionary<string, object?> { ["project"] = project, ["stack"] = stack },
            },
        };

    [Fact]
    public async Task ExposesReferencedStackOutputs()
    {
        var source = new FakeStackOutputsSource().With(
            new StackCoordinates(Org, "backend", "prod"),
            new Dictionary<string, object?> { ["url"] = "https://api" });
        var provider = Build(source);

        var output = (Dictionary<string, object?>)(await provider.OpenAsync(Inputs("api", "backend", "prod"), CancellationToken.None))!;

        var api = (IReadOnlyDictionary<string, object?>)output["api"]!;
        Assert.Equal("https://api", api["url"]);
    }

    [Fact]
    public async Task MissingStackYieldsNull()
    {
        var provider = Build(new FakeStackOutputsSource());
        var output = (Dictionary<string, object?>)(await provider.OpenAsync(Inputs("api", "backend", "prod"), CancellationToken.None))!;
        Assert.Null(output["api"]);
    }

    [Fact]
    public async Task MissingOrganizationThrows()
    {
        var provider = Build(new FakeStackOutputsSource());
        var inputs = new Dictionary<string, object?> { ["stacks"] = new Dictionary<string, object?>() };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("organization", ex.Message);
    }

    [Fact]
    public async Task SpecWithoutProjectOrStackThrows()
    {
        var provider = Build(new FakeStackOutputsSource());
        var inputs = new Dictionary<string, object?>
        {
            ["organization"] = Org,
            ["stacks"] = new Dictionary<string, object?> { ["api"] = new Dictionary<string, object?> { ["project"] = "backend" } },
        };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => provider.OpenAsync(inputs, CancellationToken.None));
        Assert.Contains("api", ex.Message);
    }
}
