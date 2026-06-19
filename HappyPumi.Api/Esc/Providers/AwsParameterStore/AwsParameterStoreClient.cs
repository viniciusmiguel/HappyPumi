#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using HappyPumi.Api.Esc.Providers.AwsSecrets;

namespace HappyPumi.Api.Esc.Providers.AwsParameterStore;

/// <summary>
/// Real <see cref="IAwsParameterStoreClient"/> backed by <c>AWSSDK.SimpleSystemsManagement</c>. Credentials
/// are resolved just-in-time: explicit access keys when supplied, otherwise the AWS default credential chain.
/// </summary>
public sealed class AwsParameterStoreClient : IAwsParameterStoreClient
{
    public async Task<string?> GetParameterAsync(AwsParameterRef reference, CancellationToken ct)
    {
        var region = RegionEndpoint.GetBySystemName(reference.Region);
        using var client = AwsCredentialsFactory.For(reference.Login) is { } credentials
            ? new AmazonSimpleSystemsManagementClient(credentials, region)
            : new AmazonSimpleSystemsManagementClient(region);
        var response = await client.GetParameterAsync(
            new GetParameterRequest { Name = reference.Name, WithDecryption = reference.WithDecryption }, ct);
        return response.Parameter?.Value;
    }
}
