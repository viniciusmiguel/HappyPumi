#nullable enable

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A stored registry artifact (package schema/index/installation, or a template archive), keyed by a
/// logical path. This is the real backing store behind the two-phase publish "pre-signed upload URLs":
/// the upload endpoints write bytes here; the download endpoints serve them.
/// </summary>
public sealed class RegistryArtifactRow
{
    /// <summary>Logical key, e.g. "packages/private/happypumi/widgets/1.0.0/schema".</summary>
    public string Key { get; set; } = default!;
    public byte[] Content { get; set; } = default!;
    public string ContentType { get; set; } = "application/octet-stream";
}
