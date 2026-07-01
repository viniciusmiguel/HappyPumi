using System;
using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for UpdateAuthPolicy + the rewired GetAuthPolicy (policy-results PR2) against real Postgres.
/// An OIDC issuer is registered (GetAuthPolicy is keyed by the issuer route), then an update persists a rule
/// set that get reflects; without an update, get returns the synthesized empty default. Unique org per test.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class AuthPolicyTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task UpdateThenGetReflectsThePolicies()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var issuerId = await RegisterIssuer(client, org);

        using var patch = await client.PatchAsJsonAsync(
            $"/api/orgs/{org}/auth/policies/{issuerId}",
            new AuthPolicyUpdateRequest
            {
                Policies = new List<AuthPolicyDefinition>
                {
                    new() { Decision = "allow", TokenType = "organization" },
                },
            });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var updated = await patch.Content.ReadFromJsonAsync<AuthPolicy>();
        Assert.Equal(issuerId, updated!.Id);
        Assert.Single(updated.Policies);

        var fetched = await client.GetFromJsonAsync<AuthPolicy>(
            $"/api/orgs/{org}/auth/policies/oidcissuers/{issuerId}");
        Assert.Single(fetched!.Policies);
        Assert.Equal("allow", fetched.Policies[0].Decision);
    }

    [Fact]
    public async Task UpdateBumpsVersionOnSecondWrite()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var issuerId = await RegisterIssuer(client, org);

        var first = await UpdatePolicies(client, org, issuerId, "allow");
        var second = await UpdatePolicies(client, org, issuerId, "deny");

        Assert.Equal(1, first.Version);
        Assert.Equal(2, second.Version);
    }

    [Fact]
    public async Task GetReturnsEmptyDefaultWhenNeverSet()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        var issuerId = await RegisterIssuer(client, org);

        var policy = await client.GetFromJsonAsync<AuthPolicy>(
            $"/api/orgs/{org}/auth/policies/oidcissuers/{issuerId}");

        Assert.Equal(issuerId, policy!.Id);
        Assert.Empty(policy.Policies);
    }

    private static async Task<AuthPolicy> UpdatePolicies(HttpClient client, string org, string id, string decision)
    {
        using var patch = await client.PatchAsJsonAsync(
            $"/api/orgs/{org}/auth/policies/{id}",
            new AuthPolicyUpdateRequest
            {
                Policies = new List<AuthPolicyDefinition> { new() { Decision = decision, TokenType = "organization" } },
            });
        patch.EnsureSuccessStatusCode();
        return (await patch.Content.ReadFromJsonAsync<AuthPolicy>())!;
    }

    private static async Task<string> RegisterIssuer(HttpClient client, string org)
    {
        using var resp = await client.PostAsJsonAsync($"/api/orgs/{org}/oidc/issuers",
            new OidcIssuerRegistrationRequest { Name = "idp", Url = "https://issuer.example" });
        resp.EnsureSuccessStatusCode();
        var issuer = await resp.Content.ReadFromJsonAsync<OidcIssuerRegistrationResponse>();
        return issuer!.Id;
    }
}
