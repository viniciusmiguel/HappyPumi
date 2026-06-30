using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>Unit tests for the in-memory IDP store (members, roles, team-role assignments; ADR-0007).</summary>
public sealed class InMemoryIdentityStoreTests
{
    private const string Org = "acme";

    [Fact]
    public void AddUpdateRemoveMember()
    {
        var store = new InMemoryIdentityStore();

        var added = store.AddMember(Org, "alice", "admin");
        Assert.Equal("admin", added.Role);
        Assert.Single(store.ListMembers(Org));

        Assert.Equal("member", store.UpdateMemberRole(Org, "alice", "member")!.Role);
        Assert.Null(store.UpdateMemberRole(Org, "ghost", "member"));

        Assert.True(store.RemoveMember(Org, "alice"));
        Assert.False(store.RemoveMember(Org, "alice"));
        Assert.Empty(store.ListMembers(Org));
    }

    [Fact]
    public void RoleCrud()
    {
        var store = new InMemoryIdentityStore();

        var role = store.CreateRole(Org, new PermissionDescriptorBase { Name = "deployer" });
        Assert.Equal("deployer", store.GetRole(Org, role.Id)!.Name);

        var updated = store.UpdateRole(Org, role.Id, new PermissionDescriptorBase { Name = "deployer-2" });
        Assert.Equal("deployer-2", updated!.Name);
        Assert.Equal(2, updated.Version); // bumped on update

        Assert.True(store.DeleteRole(Org, role.Id));
        Assert.Null(store.GetRole(Org, role.Id));
    }

    [Fact]
    public void AssignTeamRoleRequiresAnExistingRole()
    {
        var store = new InMemoryIdentityStore();
        var role = store.CreateRole(Org, new PermissionDescriptorBase { Name = "deployer" });

        Assert.False(store.AssignTeamRole(Org, "platform", "no-such-role"));
        Assert.True(store.AssignTeamRole(Org, "platform", role.Id));
        Assert.True(store.RemoveTeamRole(Org, "platform", role.Id));
        Assert.False(store.RemoveTeamRole(Org, "platform", role.Id));
    }

    [Fact]
    public void UpdateTeamChangesDisplayAndDescription()
    {
        var store = new InMemoryIdentityStore();
        store.CreateTeam(Org, "platform", "Platform", "old", "pulumi");

        var updated = store.UpdateTeam(Org, "platform", newName: null, displayName: "Platform Eng", description: "new");

        Assert.Equal("Platform Eng", updated!.DisplayName);
        Assert.Equal("new", updated.Description);
        Assert.Equal("platform", updated.Name); // unchanged when newName is null
        Assert.Null(store.UpdateTeam(Org, "ghost", null, "x", null)); // missing team
    }

    [Fact]
    public void UpdateTeamRenameCarriesMembersAndRoleGrants()
    {
        var store = new InMemoryIdentityStore();
        var role = store.CreateRole(Org, new PermissionDescriptorBase { Name = "deployer" });
        store.CreateTeam(Org, "platform", "Platform", "", "pulumi");
        store.AddTeamMember(Org, "platform", "alice");
        store.AssignTeamRole(Org, "platform", role.Id);

        var renamed = store.UpdateTeam(Org, "platform", newName: "infra", displayName: null, description: null);

        Assert.Equal("infra", renamed!.Name);
        Assert.Contains("alice", renamed.Members);
        Assert.Contains(role.Id, renamed.RoleIds);
        Assert.Null(store.GetTeam(Org, "platform")); // the old name is gone
        Assert.NotNull(store.GetTeam(Org, "infra"));
        // The role grant followed the rename, so revoking under the new name succeeds.
        Assert.True(store.RemoveTeamRole(Org, "infra", role.Id));
    }

    [Fact]
    public void UpdateTeamRenameToExistingNameReturnsNull()
    {
        var store = new InMemoryIdentityStore();
        store.CreateTeam(Org, "platform", "Platform", "", "pulumi");
        store.CreateTeam(Org, "infra", "Infra", "", "pulumi");

        Assert.Null(store.UpdateTeam(Org, "platform", newName: "infra", displayName: null, description: null));
        Assert.NotNull(store.GetTeam(Org, "platform")); // left intact on collision
    }

    [Fact]
    public void DeletingARoleDropsItsTeamGrant()
    {
        var store = new InMemoryIdentityStore();
        var role = store.CreateRole(Org, new PermissionDescriptorBase { Name = "deployer" });
        store.AssignTeamRole(Org, "platform", role.Id);

        store.DeleteRole(Org, role.Id);

        // The grant is gone, so removing it again reports "not held".
        Assert.False(store.RemoveTeamRole(Org, "platform", role.Id));
    }
}
