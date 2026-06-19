#nullable enable

using Amazon.Runtime;

namespace HappyPumi.Api.Esc.Providers.AwsSecrets;

/// <summary>
/// Builds AWS SDK credentials from an <see cref="AwsLogin"/>, shared by every AWS-backed ESC integration
/// (secrets, parameter store, IAM rotator). Returns null to fall back to the AWS default credential chain.
/// </summary>
public static class AwsCredentialsFactory
{
    public static AWSCredentials? For(AwsLogin? login)
    {
        if (login is not { AccessKeyId: { } key, SecretAccessKey: { } secret }
            || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
            return null;
        return string.IsNullOrWhiteSpace(login.SessionToken)
            ? new BasicAWSCredentials(key, secret)
            : new SessionAWSCredentials(key, secret, login.SessionToken);
    }
}
