#nullable enable

namespace HappyPumi.Api.CloudSetup;

/// <summary>
/// Resolves an <see cref="ICloudSetupProvider"/> by its key ("aws" | "azure" | "gcp"), mirroring
/// <c>IVcsProviderRegistry</c> (ADR-0009). The OAuth endpoints read the requested provider from the body
/// and delegate to the matching provider.
/// </summary>
public interface ICloudSetupProviderRegistry
{
    /// <summary>Returns the provider for the key, or null when none is wired.</summary>
    ICloudSetupProvider? For(string key);
}

/// <summary>Maps cloud-provider keys to their <see cref="ICloudSetupProvider"/> implementations.</summary>
public sealed class CloudSetupProviderRegistry(
    AwsCloudSetupProvider aws, AzureCloudSetupProvider azure, GcpCloudSetupProvider gcp) : ICloudSetupProviderRegistry
{
    public ICloudSetupProvider? For(string key) => key switch
    {
        "aws" => aws,
        "azure" => azure,
        "gcp" => gcp,
        _ => null,
    };
}
