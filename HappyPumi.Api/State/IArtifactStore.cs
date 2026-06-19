#nullable enable

namespace HappyPumi.Api.State;

/// <summary>A stored artifact's bytes + content type.</summary>
public sealed record StoredArtifact(byte[] Content, string ContentType);

/// <summary>
/// Path-keyed blob store for registry artifacts (package schema/index/installation, template archives).
/// Backs the two-phase publish flow's upload/download URLs (ADR-0005: bytes live in Postgres for the
/// in-cluster demo; a blob backend is the production follow-up).
/// </summary>
public interface IArtifactStore
{
    void Put(string key, byte[] content, string contentType);
    StoredArtifact? Get(string key);
    bool Exists(string key);
}

/// <summary>Logical keys for registry artifacts.</summary>
public static class ArtifactKeys
{
    public static string Package(string source, string publisher, string name, string version, string kind)
        => $"packages/{source}/{publisher}/{name}/{version}/{kind}";

    public static string TemplateArchive(string source, string publisher, string name, string version)
        => $"templates/{source}/{publisher}/{name}/{version}/archive";

    public static string PolicyPack(string org, string name, long version)
        => $"policypacks/{org}/{name}/{version}/pack";
}
