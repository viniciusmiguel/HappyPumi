#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.AwsSecrets;

/// <summary>Optional explicit AWS credentials for a Secrets Manager read (else the default chain is used).</summary>
public sealed record AwsLogin(string? AccessKeyId, string? SecretAccessKey, string? SessionToken);

/// <summary>A single secret to fetch: region + secret id (name or ARN), with optional explicit credentials.</summary>
public readonly record struct AwsSecretRef(string Region, string SecretId, AwsLogin? Login);

/// <summary>
/// Owned, thin seam over the AWS Secrets Manager SDK (CLAUDE.md: wrap third-party libs). Lets the
/// <see cref="AwsSecretsProvider"/> be unit-tested with a named fake instead of calling AWS.
/// </summary>
public interface IAwsSecretsClient
{
    /// <summary>Reads a secret's string value at open time. Returns null when the secret has no string value.</summary>
    Task<string?> GetSecretAsync(AwsSecretRef reference, CancellationToken ct);
}
