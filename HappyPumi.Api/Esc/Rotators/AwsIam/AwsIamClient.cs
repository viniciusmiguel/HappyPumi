#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using HappyPumi.Api.Esc.Providers.AwsSecrets;

namespace HappyPumi.Api.Esc.Rotators.AwsIam;

/// <summary>
/// Real <see cref="IAwsIamClient"/> backed by <c>AWSSDK.IdentityManagement</c>. Credentials are resolved via
/// the shared <see cref="AwsCredentialsFactory"/> (explicit keys or the default chain), just-in-time.
/// </summary>
public sealed class AwsIamClient : IAwsIamClient
{
    public async Task<AwsAccessKey> CreateAccessKeyAsync(string region, string userName, AwsLogin? login, CancellationToken ct)
    {
        using var client = Client(region, login);
        var response = await client.CreateAccessKeyAsync(new CreateAccessKeyRequest { UserName = userName }, ct);
        return new AwsAccessKey(response.AccessKey.AccessKeyId, response.AccessKey.SecretAccessKey);
    }

    public async Task DeleteAccessKeyAsync(string region, string userName, string accessKeyId, AwsLogin? login, CancellationToken ct)
    {
        using var client = Client(region, login);
        await client.DeleteAccessKeyAsync(new DeleteAccessKeyRequest { UserName = userName, AccessKeyId = accessKeyId }, ct);
    }

    private static AmazonIdentityManagementServiceClient Client(string region, AwsLogin? login)
    {
        var endpoint = RegionEndpoint.GetBySystemName(region);
        return AwsCredentialsFactory.For(login) is { } credentials
            ? new AmazonIdentityManagementServiceClient(credentials, endpoint)
            : new AmazonIdentityManagementServiceClient(endpoint);
    }
}
