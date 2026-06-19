using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>Unit tests for counting how many stacks use a registry package (the Private Components stats).</summary>
public sealed class PackageUsageTests
{
    private static StoredStack StackWith(string name, params string[] resourceTypes)
    {
        var resources = new List<Dictionary<string, object?>>();
        foreach (var t in resourceTypes)
            resources.Add(new Dictionary<string, object?> { ["type"] = t });
        return new StoredStack
        {
            Coordinates = new StackCoordinates("happypumi", "proj", name),
            Deployment = new AppUntypedDeployment
            {
                Deployment = new Dictionary<string, object?> { ["resources"] = resources },
            },
        };
    }

    [Fact]
    public void CountsStacksThatDeployAResourceFromThePackage()
    {
        var stacks = new[]
        {
            StackWith("a", "pulumi:pulumi:Stack", "widgets:index:Widget"),
            StackWith("b", "pulumi:pulumi:Stack", "widgets:index:Widget", "random:index/randomPet:RandomPet"),
            StackWith("c", "pulumi:pulumi:Stack", "aws:s3/bucket:Bucket"), // does not use widgets
        };

        Assert.Equal(2, PackageUsage.StacksUsing(stacks, "widgets"));
        Assert.Equal(1, PackageUsage.StacksUsing(stacks, "aws"));
        Assert.Equal(0, PackageUsage.StacksUsing(stacks, "gadgets"));
    }

    [Fact]
    public void DoesNotMatchOnPackageNamePrefixOfAnotherPackage()
    {
        // "widget" must not match the "widgets" package (the ':' delimiter guards against prefix collisions).
        var stacks = new[] { StackWith("a", "widgets:index:Widget") };
        Assert.Equal(0, PackageUsage.StacksUsing(stacks, "widget"));
    }

    [Fact]
    public void NeverDeployedStacksContributeZero()
    {
        var stacks = new[] { new StoredStack { Coordinates = new StackCoordinates("o", "p", "s") } };
        Assert.Equal(0, PackageUsage.StacksUsing(stacks, "widgets"));
    }
}
