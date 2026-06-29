#nullable enable

namespace HappyPumi.Api.State;

/// <summary>
/// A git source for a managed deployment (the remote-workspace path: <c>pulumi up --remote</c> /
/// auto-API <c>NewRemoteStackGitSource</c>). <paramref name="RepoDir"/> is the project subdirectory
/// within the repo; null means the repo root. <paramref name="Branch"/> null means the default branch.
/// </summary>
public sealed record GitSource(string RepoUrl, string? Branch, string? RepoDir);
