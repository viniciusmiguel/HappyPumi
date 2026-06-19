#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.SecretManager.V1;

namespace HappyPumi.Api.Esc.Providers.GcpSecrets;

/// <summary>
/// Real <see cref="IGcpSecretsClient"/> backed by <c>Google.Cloud.SecretManager.V1</c>. Authentication uses
/// Application Default Credentials (service account / workload identity / gcloud), resolved just-in-time.
/// </summary>
public sealed class GcpSecretsClient : IGcpSecretsClient
{
    public async Task<string?> AccessAsync(GcpSecretRef reference, CancellationToken ct)
    {
        var client = await SecretManagerServiceClient.CreateAsync();
        var version = string.IsNullOrWhiteSpace(reference.Version) ? "latest" : reference.Version;
        var name = new SecretVersionName(reference.ProjectId, reference.SecretId, version);
        var response = await client.AccessSecretVersionAsync(name);
        return response.Payload.Data.ToStringUtf8();
    }
}
