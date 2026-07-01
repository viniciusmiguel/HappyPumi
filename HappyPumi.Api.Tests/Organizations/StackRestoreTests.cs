using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for stack restore &amp; bulk transfer (org-admin PR5) against real Postgres. A soft-deleted
/// stack lands a tombstone that surfaces on the restore list, restores back to a live stack, and drops off
/// the list; bulk-transfer reassigns every active stack from one org to another. Unique orgs per test for
/// independence.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackRestoreTests(HappyPumiApp app)
{
    private const string Project = "webapp";

    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task DeleteRecordsTombstoneThenRestoreRecreatesTheStack()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var stack = "dev";
        SeedStack(org, stack);

        using var deleted = await client.DeleteAsync($"/api/stacks/{org}/{Project}/{stack}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var list = await client.GetFromJsonAsync<ListDeletedStacksResponse>($"/api/orgs/{org}/restore-stack");
        Assert.Single(list!.DeletedStacks);
        var tombstone = list.DeletedStacks[0];
        Assert.Equal(stack, tombstone.StackName);
        Assert.Equal(Project, tombstone.ProjectName);
        Assert.NotNull(tombstone.LastUpdate);

        using var restored = await client.PostAsJsonAsync(
            $"/api/orgs/{org}/restore-stack/{tombstone.ProgramId}", new { stackName = stack });
        Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);

        Assert.NotNull(Find(org, stack)); // the stack is live again
        var afterRestore = await client.GetFromJsonAsync<ListDeletedStacksResponse>($"/api/orgs/{org}/restore-stack");
        Assert.Empty(afterRestore!.DeletedStacks); // tombstone dropped
    }

    [Fact]
    public async Task RestoreUnknownProgramIdIs404()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        using var response = await client.PostAsJsonAsync(
            $"/api/orgs/{org}/restore-stack/ghost", new { stackName = "dev" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TransferAllStacksReassignsEveryStackToTargetOrg()
    {
        using var client = app.CreateClient();
        var fromOrg = NewOrg();
        var toOrg = NewOrg();
        SeedStack(fromOrg, "one");
        SeedStack(fromOrg, "two");

        using var response = await client.PostAsJsonAsync(
            $"/api/orgs/{fromOrg}/bulk-transfer/stacks", new { fromOrg, toOrg });
        response.EnsureSuccessStatusCode();
        var message = await response.Content.ReadAsStringAsync();
        Assert.Contains($"Transferred 2 stacks from {fromOrg} to {toOrg}", message);

        Assert.NotNull(Find(toOrg, "one"));
        Assert.NotNull(Find(toOrg, "two"));
        Assert.Null(Find(fromOrg, "one"));
    }

    private void SeedStack(string org, string stack)
    {
        using var scope = app.Services.CreateScope();
        var stacks = scope.ServiceProvider.GetRequiredService<IStackStore>();
        stacks.TryCreate(new StoredStack { Coordinates = new StackCoordinates(org, Project, stack) });
    }

    private StoredStack? Find(string org, string stack)
    {
        using var scope = app.Services.CreateScope();
        var stacks = scope.ServiceProvider.GetRequiredService<IStackStore>();
        return stacks.Find(new StackCoordinates(org, Project, stack));
    }
}
