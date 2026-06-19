#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.GcpSecrets;

/// <summary>A single GCP secret to fetch: project + secret id, with an optional version (default "latest").</summary>
public readonly record struct GcpSecretRef(string ProjectId, string SecretId, string? Version);

/// <summary>
/// Owned, thin seam over the Google Cloud Secret Manager SDK (CLAUDE.md: wrap third-party libs). Lets the
/// <see cref="GcpSecretsProvider"/> be unit-tested with a named fake instead of calling GCP.
/// </summary>
public interface IGcpSecretsClient
{
    /// <summary>Accesses a secret version's payload at open time. Returns null when it has no value.</summary>
    Task<string?> AccessAsync(GcpSecretRef reference, CancellationToken ct);
}
