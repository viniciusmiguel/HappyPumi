using System.Collections.Generic;
using System.Net.Http;
using HappyPumi.Api.CloudSetup;
using HappyPumi.Api.Tests.Esc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HappyPumi.Api.Tests.CloudSetup;

/// <summary>
/// Unit test of <see cref="AzureCloudSetupProvider.BuildAuthorizationUrl"/>: configured → a real
/// Microsoft identity-platform URL carrying the client id + state; unconfigured → empty (graceful gating).
/// </summary>
public sealed class AzureCloudSetupProviderTests
{
    private static AzureCloudSetupProvider Provider(bool configured)
    {
        var settings = new Dictionary<string, string?>();
        if (configured)
        {
            settings["CloudSetup:Azure:ClientId"] = "client-123";
            settings["CloudSetup:Azure:ClientSecret"] = "secret-abc";
            settings["CloudSetup:Azure:RedirectUri"] = "https://happypumi.test/cb";
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new AzureCloudSetupProvider(new HttpClient(new StubHttpHandler("{}")), config);
    }

    [Fact]
    public void BuildAuthorizationUrlIncludesClientAndStateWhenConfigured()
    {
        var url = Provider(configured: true).BuildAuthorizationUrl("st8", returnUrl: null);

        Assert.Contains("login.microsoftonline.com", url);
        Assert.Contains("client_id=client-123", url);
        Assert.Contains("response_type=code", url);
        Assert.Contains("state=st8", url);
    }

    [Fact]
    public void BuildAuthorizationUrlIsEmptyWhenUnconfigured()
        => Assert.Equal("", Provider(configured: false).BuildAuthorizationUrl("st8", returnUrl: null));
}
