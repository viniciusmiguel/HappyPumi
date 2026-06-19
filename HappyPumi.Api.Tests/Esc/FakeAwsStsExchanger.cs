using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.Logins.Aws;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Records the last web-identity exchange and returns canned temporary credentials.</summary>
public sealed class FakeAwsStsExchanger : IAwsStsExchanger
{
    public AwsWebIdentityRequest? LastRequest { get; private set; }

    public Task<AwsTempCredentials> AssumeRoleWithWebIdentityAsync(AwsWebIdentityRequest request, CancellationToken ct)
    {
        LastRequest = request;
        return Task.FromResult(new AwsTempCredentials("ASIAFAKE", "fake-secret", "fake-session"));
    }
}
