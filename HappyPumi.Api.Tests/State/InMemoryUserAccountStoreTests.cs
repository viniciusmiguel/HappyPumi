using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>Unit tests for the in-memory per-user account store (org-admin PR6, ADR-0005).</summary>
public sealed class InMemoryUserAccountStoreTests
{
    private const string Login = "happypumi";

    [Fact]
    public void GetReturnsDefaultsWhenAbsent()
    {
        var store = new InMemoryUserAccountStore();

        var account = store.Get(Login);

        Assert.Equal(Login, account.Login);
        Assert.True(account.VerifiedEmail); // a fresh user is considered verified
        Assert.Null(account.PendingEmail);
        Assert.Null(account.DefaultOrg);
    }

    [Fact]
    public void UpdatePersistsAndSubsequentGetReflectsIt()
    {
        var store = new InMemoryUserAccountStore();

        store.Update(Login, a =>
        {
            a.PendingEmail = "new@contoso.com";
            a.VerifiedEmail = false;
            a.DefaultOrg = "acme";
        });

        var account = store.Get(Login);
        Assert.Equal("new@contoso.com", account.PendingEmail);
        Assert.False(account.VerifiedEmail);
        Assert.Equal("acme", account.DefaultOrg);
    }
}
