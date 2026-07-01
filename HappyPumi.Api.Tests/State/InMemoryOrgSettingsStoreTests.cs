using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>Unit tests for the in-memory per-org settings store (ADR-0005).</summary>
public sealed class InMemoryOrgSettingsStoreTests
{
    private const string Org = "acme";

    [Fact]
    public void GetReturnsDefaultsWhenAbsent()
    {
        var store = new InMemoryOrgSettingsStore();

        var settings = store.Get(Org);

        Assert.Equal(Org, settings.Org);
        Assert.True(settings.MembersCanCreateStacks);
        Assert.Equal("github", settings.PreferredVcs);
        Assert.Equal("disabled", settings.AiEnablement);
        Assert.False(settings.NeoEnabled);
    }

    [Fact]
    public void UpdatePersistsAndSubsequentGetReflectsIt()
    {
        var store = new InMemoryOrgSettingsStore();

        store.Update(Org, s =>
        {
            s.MembersCanCreateStacks = false;
            s.PreferredVcs = "gitlab";
            s.DefaultRoleId = "role-123";
        });

        var settings = store.Get(Org);
        Assert.False(settings.MembersCanCreateStacks);
        Assert.Equal("gitlab", settings.PreferredVcs);
        Assert.Equal("role-123", settings.DefaultRoleId);
    }
}
