using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Esc.Providers.AwsParameterStore;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Named fake (CLAUDE.md) for <see cref="IAwsParameterStoreClient"/>: serves params from an in-memory map.</summary>
public sealed class FakeAwsParameterStoreClient : IAwsParameterStoreClient
{
    private readonly Dictionary<string, string?> _params = new();
    public List<AwsParameterRef> Requests { get; } = new();

    public FakeAwsParameterStoreClient With(string region, string name, string? value)
    {
        _params[$"{region}|{name}"] = value;
        return this;
    }

    public Task<string?> GetParameterAsync(AwsParameterRef reference, CancellationToken ct)
    {
        Requests.Add(reference);
        return Task.FromResult(_params.GetValueOrDefault($"{reference.Region}|{reference.Name}"));
    }
}
