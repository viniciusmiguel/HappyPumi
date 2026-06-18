using HappyPumi.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HappyPumi.Api.Tests;

/// <summary>
/// Boots the real HappyPumi API in-process (no sockets, no Docker) so component tests can
/// exercise endpoints against the actual FastEndpoints pipeline and wire contracts.
///
/// <example>
/// <code>
/// [Collection(HappyPumiCollection.Name)]
/// public sealed class MyTests(HappyPumiApp app)
/// {
///     [Fact] public async Task Works() => Assert.True((await app.CreateClient().GetAsync("/api/user")).IsSuccessStatusCode);
/// }
/// </code>
/// </example>
/// </summary>
public sealed class HappyPumiApp : WebApplicationFactory<ApiMarker>;

/// <summary>
/// Shares one <see cref="HappyPumiApp"/> across a test class so the host is built once.
/// Use the collection name on test classes: <c>[Collection(HappyPumiCollection.Name)]</c>.
/// </summary>
[CollectionDefinition(Name)]
public sealed class HappyPumiCollection : ICollectionFixture<HappyPumiApp>
{
    public const string Name = "happypumi-api";
}
