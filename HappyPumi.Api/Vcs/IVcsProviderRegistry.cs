#nullable enable

namespace HappyPumi.Api.Vcs;

/// <summary>
/// Resolves an <see cref="IVcsProvider"/> for an integration's <c>Kind</c> (ADR-0009). The generic
/// repo/branch endpoints load the integration record, ask the registry for its provider, and delegate.
/// </summary>
public interface IVcsProviderRegistry
{
    /// <summary>Returns the provider for the kind, or null when none is wired (e.g. azure-devops lands in PR3).</summary>
    IVcsProvider? For(string kind);
}

/// <summary>Maps integration kinds to providers. GitHub serves both <c>github</c> and <c>github-enterprise</c>.</summary>
public sealed class VcsProviderRegistry(GitHubVcsProvider github) : IVcsProviderRegistry
{
    public IVcsProvider? For(string kind) =>
        kind is "github" or "github-enterprise" ? github : null;
}
