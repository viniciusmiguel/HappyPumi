#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace HappyPumi.Api.Esc.Providers.Logins.Aws;

/// <summary>
/// Real <see cref="IAwsStsExchanger"/> backed by <c>AWSSDK.SecurityToken</c>. AssumeRoleWithWebIdentity is
/// authenticated by the OIDC token itself, so the STS client needs no AWS credentials (anonymous). The role's
/// trust policy must federate HappyPumi's OIDC issuer + audience for the call to succeed.
/// </summary>
public sealed class AwsStsExchanger : IAwsStsExchanger
{
    public async Task<AwsTempCredentials> AssumeRoleWithWebIdentityAsync(AwsWebIdentityRequest request, CancellationToken ct)
    {
        var region = string.IsNullOrWhiteSpace(request.Region)
            ? RegionEndpoint.USEast1
            : RegionEndpoint.GetBySystemName(request.Region);
        using var sts = new AmazonSecurityTokenServiceClient(new AnonymousAWSCredentials(), region);

        var response = await sts.AssumeRoleWithWebIdentityAsync(new AssumeRoleWithWebIdentityRequest
        {
            RoleArn = request.RoleArn,
            RoleSessionName = request.SessionName,
            WebIdentityToken = request.WebIdentityToken,
            DurationSeconds = request.DurationSeconds ?? 3600,
        }, ct);

        var c = response.Credentials
            ?? throw new InvalidOperationException($"AWS STS returned no credentials for role '{request.RoleArn}'.");
        return new AwsTempCredentials(c.AccessKeyId, c.SecretAccessKey, c.SessionToken);
    }
}
