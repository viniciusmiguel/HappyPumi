#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Esc.Providers.Logins.Aws;

/// <summary>A web-identity exchange request: present <paramref name="WebIdentityToken"/> to assume <paramref name="RoleArn"/>.</summary>
public readonly record struct AwsWebIdentityRequest(
    string RoleArn,
    string SessionName,
    string WebIdentityToken,
    string? Region,
    int? DurationSeconds);

/// <summary>The temporary, time-limited credentials AWS STS vends for an assumed role.</summary>
public readonly record struct AwsTempCredentials(
    string AccessKeyId,
    string SecretAccessKey,
    string SessionToken);

/// <summary>
/// Exchanges a Pulumi-ESC OIDC token for temporary AWS credentials via STS <c>AssumeRoleWithWebIdentity</c>.
/// Owned interface over the AWS SDK (CLAUDE.md) so the federation flow is testable with a fake.
/// </summary>
public interface IAwsStsExchanger
{
    Task<AwsTempCredentials> AssumeRoleWithWebIdentityAsync(AwsWebIdentityRequest request, CancellationToken ct);
}
