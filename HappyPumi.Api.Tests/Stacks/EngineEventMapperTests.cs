using System.Collections.Generic;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Stacks;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Unit tests for the engine-event projection helper: the resource count an update/preview summary
/// reports, derived from the final SummaryEvent or, failing that, distinct ResourcePreEvent resources.
/// </summary>
public sealed class EngineEventMapperTests
{
    [Fact]
    public void ResourceCountPrefersSummaryEventChangeTotals()
    {
        var events = new List<AppEngineEvent>
        {
            Pre("urn:a"),
            new() { SummaryEvent = new AppSummaryEvent { ResourceChanges = new() { ["create"] = 3, ["update"] = 1 } } },
        };

        Assert.Equal(4, EngineEventMapper.ResourceCount(events));
    }

    [Fact]
    public void ResourceCountFallsBackToDistinctResourcePreUrns()
    {
        var events = new List<AppEngineEvent> { Pre("urn:a"), Pre("urn:b"), Pre("urn:a") };

        Assert.Equal(2, EngineEventMapper.ResourceCount(events));
    }

    [Fact]
    public void ResourceCountIsZeroWithoutEvents()
        => Assert.Equal(0, EngineEventMapper.ResourceCount(new List<AppEngineEvent>()));

    private static AppEngineEvent Pre(string urn) => new()
    {
        ResourcePreEvent = new AppResourcePreEvent { Metadata = new AppStepEventMetadata { Urn = urn } },
    };
}
