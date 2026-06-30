using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.State;

/// <summary>
/// Unit tests for the in-memory deployment store (the default <see cref="IDeploymentStore"/>, ADR-0005).
/// Each test uses a fresh instance. Focused on the remote-workspace git source persisted by
/// CreateAPIDeploymentHandlerV2 for the runner's git-clone job.
/// </summary>
public sealed class InMemoryDeploymentStoreTests
{
    private static StackCoordinates Coords() => new("organization", "infra", "prod");

    [Fact]
    public void CreateDeploymentPersistsGitSource()
    {
        var store = new InMemoryDeploymentStore();
        var git = new GitSource("https://example.test/r.git", "main", "app");

        var created = store.CreateDeployment(Coords(), "update", git);

        Assert.Equal("https://example.test/r.git", created.GitRepoUrl);
        Assert.Equal("main", created.GitBranch);
        Assert.Equal("app", created.GitRepoDir);

        var fetched = store.GetById(Coords(), created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("https://example.test/r.git", fetched!.GitRepoUrl);
        Assert.Equal("main", fetched.GitBranch);
        Assert.Equal("app", fetched.GitRepoDir);
    }

    [Fact]
    public void CreateDeploymentWithoutGitLeavesSourceNull()
    {
        var store = new InMemoryDeploymentStore();

        var created = store.CreateDeployment(Coords(), "update");

        Assert.Null(created.GitRepoUrl);
        Assert.Null(created.GitBranch);
        Assert.Null(created.GitRepoDir);
    }

    [Fact]
    public void CreateDeploymentWithTemplateRefIsUnaffectedByGitFields()
    {
        var store = new InMemoryDeploymentStore();

        var created = store.CreateDeployment(Coords(), "update", git: null, templateRef: "private/acme/widget/1.0.0");

        Assert.Equal("private/acme/widget/1.0.0", created.TemplateRef);
        Assert.Null(created.GitRepoUrl);
    }
}
