using System.Text;
using System.Text.Json;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Organizations;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the change-gate CRUD endpoints (change-requests PR1) against the real Postgres-backed
/// <c>IChangeGateStore</c>: create an approval_required gate, read it back, update it, list with/without the
/// entityType/qualifiedName filters, and delete it (204 then 404). Unique org per test for independence.
/// The polymorphic gate contracts are (de)serialized with <see cref="ChangeGateJson.Options"/> because the
/// default STJ binder rejects their discriminator/property-name collision (a generator quirk, see the PR).
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class ChangeGateTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    private static CreateChangeGateRequest SampleGate(string name, long approvals, bool enabled = true) => new()
    {
        Enabled = enabled,
        Name = name,
        Rule = new ChangeGateApprovalRuleInput
        {
            RuleType = "approval_required",
            NumApprovalsRequired = approvals,
            AllowSelfApproval = false,
            RequireReapprovalOnChange = true,
            EligibleApprovers = new()
            {
                new ApprovalRuleEligibilityInputUser { EligibilityType = "specific_user", UserLogin = "alice" },
            },
        },
        Target = new ChangeGateTargetInput
        {
            EntityType = "environment", ActionTypes = new() { "update" }, QualifiedName = "proj/env",
        },
    };

    [Fact]
    public async Task CreateReadUpdateListDeleteLifecycle()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();

        var created = await PostGate(client, $"/api/change-gates/{org}", SampleGate("prod-gate", 2));
        Assert.False(string.IsNullOrWhiteSpace(created.Id));

        var read = await GetGate(client, $"/api/change-gates/{org}/{created.Id}");
        Assert.True(read.Enabled);
        Assert.Equal("environment", read.Target.EntityType);
        Assert.Contains("update", read.Target.ActionTypes);
        Assert.Equal(2, ((ChangeGateApprovalRuleOutput)read.Rule).NumApprovalsRequired);

        using var put = await Put(client, $"/api/change-gates/{org}/{created.Id}",
            ToUpdate(SampleGate("prod-gate", 3, enabled: false)));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var afterUpdate = await GetGate(client, $"/api/change-gates/{org}/{created.Id}");
        Assert.False(afterUpdate.Enabled);
        Assert.Equal(3, ((ChangeGateApprovalRuleOutput)afterUpdate.Rule).NumApprovalsRequired);

        var list = await GetList(client, $"/api/change-gates/{org}");
        Assert.Contains(list.Gates, g => g.Id == created.Id);

        using var deleted = await client.DeleteAsync($"/api/change-gates/{org}/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var gone = await client.GetAsync($"/api/change-gates/{org}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task ListFiltersByEntityTypeAndQualifiedName()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();
        var created = await PostGate(client, $"/api/change-gates/{org}", SampleGate("env-gate", 1));

        var byEnv = await GetList(client, $"/api/change-gates/{org}?entityType=environment&qualifiedName=proj/env");
        Assert.Contains(byEnv.Gates, g => g.Id == created.Id);

        var byOther = await GetList(client, $"/api/change-gates/{org}?entityType=stack");
        Assert.DoesNotContain(byOther.Gates, g => g.Id == created.Id);

        var byName = await GetList(client, $"/api/change-gates/{org}?qualifiedName=other/env");
        Assert.DoesNotContain(byName.Gates, g => g.Id == created.Id);
    }

    [Fact]
    public async Task BlankNameIsRejected()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();

        using var response = await Post(client, $"/api/change-gates/{org}", SampleGate("", 1));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAndDeleteMissingGateAre404()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();

        using var put = await Put(client, $"/api/change-gates/{org}/ghost", ToUpdate(SampleGate("x", 1)));
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);

        using var delete = await client.DeleteAsync($"/api/change-gates/{org}/ghost");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    private static UpdateChangeGateRequest ToUpdate(CreateChangeGateRequest c) => new()
    {
        Enabled = c.Enabled, Name = c.Name, Rule = c.Rule, Target = c.Target,
    };

    private static StringContent Json(object body)
        => new(ChangeGateJson.Serialize(body), Encoding.UTF8, "application/json");

    private static Task<HttpResponseMessage> Post(HttpClient client, string url, object body)
        => client.PostAsync(url, Json(body));

    private static Task<HttpResponseMessage> Put(HttpClient client, string url, object body)
        => client.PutAsync(url, Json(body));

    private static async Task<ChangeGate> PostGate(HttpClient client, string url, object body)
    {
        using var response = await Post(client, url, body);
        response.EnsureSuccessStatusCode();
        return Deserialize<ChangeGate>(await response.Content.ReadAsStringAsync());
    }

    private static async Task<ChangeGate> GetGate(HttpClient client, string url)
        => Deserialize<ChangeGate>(await client.GetStringAsync(url));

    private static async Task<ListChangeGatesResponse> GetList(HttpClient client, string url)
        => Deserialize<ListChangeGatesResponse>(await client.GetStringAsync(url));

    private static T Deserialize<T>(string raw)
        => JsonSerializer.Deserialize<T>(raw, ChangeGateJson.Options)!;
}
