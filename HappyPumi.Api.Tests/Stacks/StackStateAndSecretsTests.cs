using System.Text;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Stacks;

/// <summary>
/// Component tests for the Tier-1b state export/import and service-managed secrets endpoints
/// (ENDPOINTS.md). These exercise the wire shapes the CLI's serviceCrypter and state migration use,
/// including the base64 encoding of the byte[] secret fields.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class StackStateAndSecretsTests(HappyPumiApp app)
{
    private const string Org = "happypumi";
    private const string Project = "webapp";

    private static string Base(string stack) => $"/api/stacks/{Org}/{Project}/{stack}";

    private static async Task<string> NewStack(HttpClient client)
    {
        var stack = $"state-{Guid.NewGuid():N}";
        using var _ = await client.PostAsJsonAsync(
            $"/api/stacks/{Org}/{Project}", new AppCreateStackRequest { StackName = stack });
        return stack;
    }

    [Fact]
    public async Task ExportFreshStackReturnsEmptyV3Deployment()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);

        var deployment = await client.GetFromJsonAsync<AppUntypedDeployment>($"{Base(stack)}/export");

        Assert.NotNull(deployment);
        Assert.Equal(3, deployment!.Version);
        Assert.NotNull(deployment.Deployment); // a valid (empty) body, not null/absent
    }

    [Fact]
    public async Task ExportUnknownStackReturns404()
    {
        using var client = app.CreateClient();

        using var response = await client.GetAsync($"{Base($"ghost-{Guid.NewGuid():N}")}/export");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ImportThenExportRoundTripsTheDeployment()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        var body = new AppImportStackRequest
        {
            Version = 3,
            Deployment = new Dictionary<string, object?> { ["manifest"] = new Dictionary<string, object?>() },
        };

        using var imported = await client.PostAsJsonAsync($"{Base(stack)}/import", body);
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);

        var exported = await client.GetFromJsonAsync<AppUntypedDeployment>($"{Base(stack)}/export");
        Assert.Equal(3, exported!.Version);
    }

    [Fact]
    public async Task EncryptThenDecryptRoundTripsThroughTheApi()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        var plaintext = Encoding.UTF8.GetBytes("s3cr3t");

        var encrypted = await Post<AppEncryptValueResponse>(
            client, $"{Base(stack)}/encrypt", new AppEncryptValueRequest { Plaintext = plaintext });
        var decrypted = await Post<AppDecryptValueResponse>(
            client, $"{Base(stack)}/decrypt", new AppDecryptValueRequest { Ciphertext = encrypted.Ciphertext });

        Assert.Equal(plaintext, decrypted.Plaintext);
    }

    [Fact]
    public async Task BatchEncryptThenBatchDecryptRoundTrips()
    {
        using var client = app.CreateClient();
        var stack = await NewStack(client);
        var plaintexts = new List<byte[]>
        {
            Encoding.UTF8.GetBytes("one"),
            Encoding.UTF8.GetBytes("two"),
        };

        var encrypted = await Post<AppBatchEncryptResponse>(
            client, $"{Base(stack)}/batch-encrypt", new AppBatchEncryptRequest { Plaintexts = plaintexts });
        Assert.Equal(2, encrypted.Ciphertexts.Count);

        var decrypted = await Post<AppBatchDecryptResponse>(
            client, $"{Base(stack)}/batch-decrypt", new AppBatchDecryptRequest { Ciphertexts = encrypted.Ciphertexts });

        // Plaintexts are keyed by the base64 of each ciphertext.
        foreach (var (plain, cipher) in plaintexts.Zip(encrypted.Ciphertexts))
            Assert.Equal(plain, decrypted.Plaintexts[Convert.ToBase64String(cipher)]);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
