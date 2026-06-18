using HappyPumi.Api.Miscellaneous;

namespace HappyPumi.Api.Tests.Miscellaneous;

/// <summary>
/// Pure unit tests for the RFC 8693 request rules of POST /api/oauth/token (Tier 0). No HTTP host:
/// these pin the wire-contract validation the CLI's OIDC login depends on.
/// </summary>
public sealed class TokenExchangeTests
{
    private static Dictionary<string, object> ValidBody() => new()
    {
        ["grant_type"] = TokenExchange.GrantType,
        ["subject_token"] = "eyJ.fake.jwt",
        ["subject_token_type"] = TokenExchange.SubjectTokenType,
        ["requested_token_type"] = "urn:pulumi:token-type:access_token:organization",
        ["audience"] = "urn:pulumi:org:happypumi",
    };

    [Fact]
    public void WellFormedRequestHasNoErrors()
    {
        Assert.Empty(TokenExchange.Validate(ValidBody()));
    }

    [Theory]
    [InlineData("grant_type")]
    [InlineData("subject_token")]
    [InlineData("subject_token_type")]
    [InlineData("requested_token_type")]
    [InlineData("audience")]
    public void MissingRequiredFieldIsReported(string field)
    {
        var body = ValidBody();
        body.Remove(field);

        var errors = TokenExchange.Validate(body);

        Assert.Contains(errors, e => e.Field == field);
    }

    [Fact]
    public void WrongGrantTypeIsRejected()
    {
        var body = ValidBody();
        body["grant_type"] = "authorization_code";

        Assert.Contains(TokenExchange.Validate(body), e => e.Field == "grant_type");
    }

    [Fact]
    public void NonOrgAudienceIsRejected()
    {
        var body = ValidBody();
        body["audience"] = "urn:pulumi:team:happypumi";

        Assert.Contains(TokenExchange.Validate(body), e => e.Field == "audience");
    }

    [Fact]
    public void UnknownRequestedTokenTypeIsRejected()
    {
        var body = ValidBody();
        body["requested_token_type"] = "urn:pulumi:token-type:access_token:galaxy";

        Assert.Contains(TokenExchange.Validate(body), e => e.Field == "requested_token_type");
    }
}
