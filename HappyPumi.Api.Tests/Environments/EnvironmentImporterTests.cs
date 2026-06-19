using System.Collections.Generic;
using HappyPumi.Api.Endpoints.Environments;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Environments;

/// <summary>Unit tests for import resolution and deep-merge precedence.</summary>
public sealed class EnvironmentImporterTests
{
    private static readonly EnvCoordinates App = new("org", "proj", "app");

    // Resolver for a single imported "base" environment; anything else is missing.
    private static Dictionary<string, object?>? ResolveBase(EnvCoordinates c) =>
        c.Name == "base"
            ? EnvironmentEvaluator.ParseRoot("values:\n  a: base\n  shared:\n    x: 1\n")
            : null;

    [Fact]
    public void ImportedValuesDeepMergeAndImporterWins()
    {
        var root = EnvironmentEvaluator.ParseRoot("""
        imports:
          - base
        values:
          a: app
          shared:
            y: 2
        """);

        var merged = EnvironmentImporter.MergedValues(App, root, ResolveBase);

        Assert.Equal("app", merged["a"]); // the importing environment overrides the import
        var shared = (Dictionary<string, object?>)merged["shared"]!;
        Assert.Equal("1", shared["x"]); // kept from base
        Assert.Equal("2", shared["y"]); // added by importer (deep merge, not replace)
    }

    [Fact]
    public void MissingImportIsSkipped()
    {
        var root = EnvironmentEvaluator.ParseRoot("imports:\n  - nope\nvalues:\n  a: app\n");
        var merged = EnvironmentImporter.MergedValues(App, root, ResolveBase);
        Assert.Equal("app", merged["a"]);
    }

    [Fact]
    public void ImportCycleTerminates()
    {
        // self-import: the cycle guard must stop rather than recurse forever.
        Dictionary<string, object?>? resolve(EnvCoordinates c) =>
            EnvironmentEvaluator.ParseRoot("imports:\n  - app\nvalues:\n  a: 1\n");
        var root = resolve(App)!;
        var merged = EnvironmentImporter.MergedValues(App, root, resolve);
        Assert.Equal("1", merged["a"]);
    }
}
