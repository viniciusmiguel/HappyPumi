using System.Collections.Generic;
using System.Text;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the Settings-cluster PR4 project-scoped crypto endpoints
/// (/api/projects/{org}/{project}/encrypt|decrypt|batch-decrypt). They reuse the service-wide value crypter,
/// so /encrypt followed by /decrypt round-trips the original bytes and batch-decrypt returns each plaintext
/// keyed by the base64 of its ciphertext.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class ProjectValueCryptoTests(HappyPumiApp app)
{
    private const string Org = "acme";
    private const string Project = "widgets";

    [Fact]
    public async Task EncryptThenDecryptRoundTripsTheBytes()
    {
        using var client = app.CreateClient();
        var plaintext = Encoding.UTF8.GetBytes("hunter2");

        using var enc = await client.PostAsJsonAsync($"/api/projects/{Org}/{Project}/encrypt", new { plaintext });
        Assert.Equal(HttpStatusCode.OK, enc.StatusCode);
        var ciphertext = (await enc.Content.ReadFromJsonAsync<AppEncryptValueResponse>())!.Ciphertext;
        Assert.NotEqual(plaintext, ciphertext);

        using var dec = await client.PostAsJsonAsync($"/api/projects/{Org}/{Project}/decrypt", new { ciphertext });
        Assert.Equal(HttpStatusCode.OK, dec.StatusCode);
        var roundTripped = (await dec.Content.ReadFromJsonAsync<AppDecryptValueResponse>())!.Plaintext;
        Assert.Equal(plaintext, roundTripped);
    }

    [Fact]
    public async Task BatchDecryptReturnsPlaintextsKeyedByBase64Ciphertext()
    {
        using var client = app.CreateClient();
        var first = await Encrypt(client, "alpha");
        var second = await Encrypt(client, "beta");

        using var resp = await client.PostAsJsonAsync($"/api/projects/{Org}/{Project}/batch-decrypt",
            new { ciphertexts = new[] { first, second } });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var plaintexts = (await resp.Content.ReadFromJsonAsync<AppBatchDecryptResponse>())!.Plaintexts;
        Assert.Equal("alpha", Encoding.UTF8.GetString(plaintexts[Convert.ToBase64String(first)]));
        Assert.Equal("beta", Encoding.UTF8.GetString(plaintexts[Convert.ToBase64String(second)]));
    }

    [Fact]
    public async Task EncryptRejectsMalformedBody()
    {
        using var client = app.CreateClient();
        using var resp = await client.PostAsJsonAsync($"/api/projects/{Org}/{Project}/encrypt", new { wrong = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private async Task<byte[]> Encrypt(HttpClient client, string value)
    {
        var plaintext = Encoding.UTF8.GetBytes(value);
        using var resp = await client.PostAsJsonAsync($"/api/projects/{Org}/{Project}/encrypt", new { plaintext });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<AppEncryptValueResponse>())!.Ciphertext;
    }
}
