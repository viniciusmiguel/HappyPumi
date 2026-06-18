using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Miscellaneous;

/// <summary>
/// Component tests for POST /api/oauth/token (Tier 0, RFC 8693 token exchange). Covers the happy-path
/// issuance and the 400 rejection of a malformed request through the full HTTP pipeline.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class TokenTests(HappyPumiApp app)
{
    private static Dictionary<string, string> ValidBody() => new()
    {
        ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
        ["subject_token"] = "eyJ.fake.jwt",
        ["subject_token_type"] = "urn:ietf:params:oauth:token-type:id_token",
        ["requested_token_type"] = "urn:pulumi:token-type:access_token:organization",
        ["audience"] = "urn:pulumi:org:happypumi",
    };

    [Fact]
    public async Task WellFormedRequestIssuesAnAccessToken()
    {
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/oauth/token", ValidBody());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grant = await response.Content.ReadFromJsonAsync<TokenExchangeGrantResponse>();
        Assert.NotNull(grant);
        Assert.False(string.IsNullOrWhiteSpace(grant!.AccessToken));
        Assert.Equal("Bearer", grant.TokenType);
        Assert.Equal("urn:pulumi:token-type:access_token:organization", grant.IssuedTokenType);
    }

    [Fact]
    public async Task MalformedRequestIsRejectedWith400()
    {
        using var client = app.CreateClient();
        var body = ValidBody();
        body.Remove("subject_token");

        using var response = await client.PostAsJsonAsync("/api/oauth/token", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
