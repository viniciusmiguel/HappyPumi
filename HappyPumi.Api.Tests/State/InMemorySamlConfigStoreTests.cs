using System.Linq;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for <see cref="InMemorySamlConfigStore"/>: upsert/Get round-trips; a second upsert replaces by
/// org; AddAdmin appends (and is false when no config exists yet, idempotent on duplicates); ListAdmins.
/// </summary>
public sealed class InMemorySamlConfigStoreTests
{
    private static StoredSamlConfig Config(string org, string sso = "https://idp/sso") => new()
    {
        Org = org, IdpMetadataXml = "<xml/>", SsoUrl = sso, Certificate = "cert", Enabled = true,
    };

    [Fact]
    public void UpsertThenGetRoundTrips()
    {
        var store = new InMemorySamlConfigStore();
        store.Upsert(Config("acme"));

        var got = store.Get("acme");
        Assert.NotNull(got);
        Assert.Equal("https://idp/sso", got!.SsoUrl);
        Assert.True(got.Enabled);
    }

    [Fact]
    public void GetIsNullWhenUnconfigured()
        => Assert.Null(new InMemorySamlConfigStore().Get("ghost"));

    [Fact]
    public void SecondUpsertReplacesByOrg()
    {
        var store = new InMemorySamlConfigStore();
        store.Upsert(Config("acme", "https://old/sso"));
        store.Upsert(Config("acme", "https://new/sso"));

        Assert.Equal("https://new/sso", store.Get("acme")!.SsoUrl);
    }

    [Fact]
    public void AddAdminIsFalseWithoutConfig()
        => Assert.False(new InMemorySamlConfigStore().AddAdmin("acme", "alice"));

    [Fact]
    public void AddAdminAppendsAndListAdminsReflectsIt()
    {
        var store = new InMemorySamlConfigStore();
        store.Upsert(Config("acme"));

        Assert.True(store.AddAdmin("acme", "alice"));
        Assert.True(store.AddAdmin("acme", "bob"));
        Assert.True(store.AddAdmin("acme", "alice")); // duplicate is idempotent

        var admins = store.ListAdmins("acme");
        Assert.Equal(new[] { "alice", "bob" }, admins.ToArray());
    }

    [Fact]
    public void ListAdminsIsEmptyWithoutConfig()
        => Assert.Empty(new InMemorySamlConfigStore().ListAdmins("ghost"));
}
