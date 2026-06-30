using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for the in-memory stack-permission store (the default <see cref="IStackPermissionStore"/>,
/// ADR-0005). Each test uses a fresh instance, so they are fully isolated.
/// </summary>
public sealed class InMemoryStackPermissionStoreTests
{
    private static StackCoordinates Coords(string stack = "dev") => new("happypumi", "webapp", stack);

    [Fact]
    public void SetUserPermissionIsListedAndReadBack()
    {
        var store = new InMemoryStackPermissionStore();
        store.SetUserPermission(Coords(), "alice", StackPermissionLevel.Write);

        Assert.Equal(StackPermissionLevel.Write, store.GetUserPermission(Coords(), "alice"));
        var users = store.ListUsers(Coords());
        Assert.Equal(("alice", StackPermissionLevel.Write), Assert.Single(users));
    }

    [Fact]
    public void SetUserPermissionOverwritesExistingGrant()
    {
        var store = new InMemoryStackPermissionStore();
        store.SetUserPermission(Coords(), "alice", StackPermissionLevel.Read);
        store.SetUserPermission(Coords(), "alice", StackPermissionLevel.Admin);

        Assert.Equal(StackPermissionLevel.Admin, store.GetUserPermission(Coords(), "alice"));
        Assert.Single(store.ListUsers(Coords()));
    }

    [Fact]
    public void GetUserPermissionIsNullWhenNoGrant()
        => Assert.Null(new InMemoryStackPermissionStore().GetUserPermission(Coords(), "ghost"));

    [Fact]
    public void RemoveUserReturnsTrueOnlyWhenPresent()
    {
        var store = new InMemoryStackPermissionStore();
        store.SetUserPermission(Coords(), "alice", StackPermissionLevel.Read);

        Assert.True(store.RemoveUser(Coords(), "alice"));
        Assert.False(store.RemoveUser(Coords(), "alice"));
        Assert.Null(store.GetUserPermission(Coords(), "alice"));
    }

    [Fact]
    public void GetStackCreatorReturnsTheCreatorGrant()
    {
        var store = new InMemoryStackPermissionStore();
        store.SetUserPermission(Coords(), "owner", StackPermissionLevel.Admin, isCreator: true);
        store.SetUserPermission(Coords(), "alice", StackPermissionLevel.Read);

        Assert.Equal("owner", store.GetStackCreator(Coords()));
    }

    [Fact]
    public void GetStackCreatorIsNullWhenNoCreatorRecorded()
    {
        var store = new InMemoryStackPermissionStore();
        store.SetUserPermission(Coords(), "alice", StackPermissionLevel.Read);

        Assert.Null(store.GetStackCreator(Coords()));
    }

    [Fact]
    public void TeamGrantsAreListedSeparatelyFromUsers()
    {
        var store = new InMemoryStackPermissionStore();
        store.SetUserPermission(Coords(), "shared", StackPermissionLevel.Read);
        store.SetTeamPermission(Coords(), "shared", StackPermissionLevel.Admin);

        Assert.Equal(("shared", StackPermissionLevel.Admin), Assert.Single(store.ListTeams(Coords())));
        Assert.Equal(("shared", StackPermissionLevel.Read), Assert.Single(store.ListUsers(Coords())));
    }

    [Fact]
    public void SetTeamPermissionOverwritesAndRemoveDeletes()
    {
        var store = new InMemoryStackPermissionStore();
        store.SetTeamPermission(Coords(), "eng", StackPermissionLevel.Read);
        store.SetTeamPermission(Coords(), "eng", StackPermissionLevel.Write);

        Assert.Equal(("eng", StackPermissionLevel.Write), Assert.Single(store.ListTeams(Coords())));
        Assert.True(store.RemoveTeam(Coords(), "eng"));
        Assert.False(store.RemoveTeam(Coords(), "eng"));
        Assert.Empty(store.ListTeams(Coords()));
    }

    [Fact]
    public void GrantsAreScopedPerStack()
    {
        var store = new InMemoryStackPermissionStore();
        store.SetUserPermission(Coords("dev"), "alice", StackPermissionLevel.Read);

        Assert.Empty(store.ListUsers(Coords("prod")));
        Assert.Null(store.GetUserPermission(Coords("prod"), "alice"));
    }
}
