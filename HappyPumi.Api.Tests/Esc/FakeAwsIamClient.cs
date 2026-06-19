using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.AwsSecrets;
using HappyPumi.Api.Esc.Rotators.AwsIam;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IAwsIamClient"/>: mints a fixed new key and records calls.</summary>
public sealed class FakeAwsIamClient : IAwsIamClient
{
    public AwsAccessKey NextKey { get; set; } = new("AKIA-NEW", "secret-new");
    public List<string> CreatedFor { get; } = new();
    public List<string> Deleted { get; } = new();

    public Task<AwsAccessKey> CreateAccessKeyAsync(string region, string userName, AwsLogin? login, CancellationToken ct)
    {
        CreatedFor.Add(userName);
        return Task.FromResult(NextKey);
    }

    public Task DeleteAccessKeyAsync(string region, string userName, string accessKeyId, AwsLogin? login, CancellationToken ct)
    {
        Deleted.Add(accessKeyId);
        return Task.CompletedTask;
    }
}
