#nullable enable

using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.AwsSecrets;

namespace HappyPumi.Api.Esc.Rotators.AwsIam;

/// <summary>A freshly created IAM access key pair.</summary>
public readonly record struct AwsAccessKey(string AccessKeyId, string SecretAccessKey);

/// <summary>
/// Owned, thin seam over the AWS IAM SDK (CLAUDE.md: wrap third-party libs), used by the aws-iam rotator to
/// create a new access key and retire the previous one. Faked in tests instead of calling AWS.
/// </summary>
public interface IAwsIamClient
{
    Task<AwsAccessKey> CreateAccessKeyAsync(string region, string userName, AwsLogin? login, CancellationToken ct);
    Task DeleteAccessKeyAsync(string region, string userName, string accessKeyId, AwsLogin? login, CancellationToken ct);
}
