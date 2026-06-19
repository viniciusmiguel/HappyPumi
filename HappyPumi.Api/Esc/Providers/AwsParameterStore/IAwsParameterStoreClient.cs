#nullable enable

using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.AwsSecrets;

namespace HappyPumi.Api.Esc.Providers.AwsParameterStore;

/// <summary>A single SSM parameter to fetch: region + name, decryption flag, and optional explicit credentials.</summary>
public readonly record struct AwsParameterRef(string Region, string Name, bool WithDecryption, AwsLogin? Login);

/// <summary>
/// Owned, thin seam over the AWS Systems Manager Parameter Store SDK (CLAUDE.md: wrap third-party libs).
/// Reuses <see cref="AwsLogin"/> (the same AWS credentials shape as aws-secrets).
/// </summary>
public interface IAwsParameterStoreClient
{
    /// <summary>Reads a parameter's value at open time. Returns null when the parameter has no value.</summary>
    Task<string?> GetParameterAsync(AwsParameterRef reference, CancellationToken ct);
}
