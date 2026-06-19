#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace HappyPumi.Api.Esc.Providers.AwsSecrets;

/// <summary>
/// Real <see cref="IAwsSecretsClient"/> backed by <c>AWSSDK.SecretsManager</c>. Credentials are resolved
/// just-in-time per ESC: explicit access keys when the definition supplies them, otherwise the AWS default
/// credential chain (env vars / profile / instance or web-identity role). Nothing is cached.
/// </summary>
public sealed class AwsSecretsClient : IAwsSecretsClient
{
    public async Task<string?> GetSecretAsync(AwsSecretRef reference, CancellationToken ct)
    {
        var region = RegionEndpoint.GetBySystemName(reference.Region);
        using var client = CredentialsFor(reference.Login) is { } credentials
            ? new AmazonSecretsManagerClient(credentials, region)
            : new AmazonSecretsManagerClient(region);
        var response = await client.GetSecretValueAsync(new GetSecretValueRequest { SecretId = reference.SecretId }, ct);
        return response.SecretString;
    }

    private static AWSCredentials? CredentialsFor(AwsLogin? login)
    {
        if (login is not { AccessKeyId: { } key, SecretAccessKey: { } secret }
            || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
            return null; // fall back to the default credential chain
        return string.IsNullOrWhiteSpace(login.SessionToken)
            ? new BasicAWSCredentials(key, secret)
            : new SessionAWSCredentials(key, secret, login.SessionToken);
    }
}
