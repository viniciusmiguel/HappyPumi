using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for Tier 5a (ENDPOINTS.md): policy groups CRUD, policy-pack publish/list/get/delete,
/// the config-schema endpoint, and the stack policy-packs listing. Unique org per test (shared store).
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class PolicyTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task PolicyGroupCreateListGetDelete()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        using var create = await client.PostAsJsonAsync($"/api/orgs/{org}/policygroups",
            new Dictionary<string, string> { ["name"] = "prod" });
        Assert.Equal(HttpStatusCode.NoContent, create.StatusCode);

        using var dup = await client.PostAsJsonAsync($"/api/orgs/{org}/policygroups",
            new Dictionary<string, string> { ["name"] = "prod" });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        var list = await client.GetFromJsonAsync<AppListPolicyGroupsResponse>($"/api/orgs/{org}/policygroups");
        Assert.Contains(list!.PolicyGroups, g => g.Name == "prod");

        var group = await client.GetFromJsonAsync<PolicyGroup>($"/api/orgs/{org}/policygroups/prod");
        Assert.Equal("prod", group!.Name);

        using var delete = await client.DeleteAsync($"/api/orgs/{org}/policygroups/prod");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task PolicyPackPublishListGetDelete()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        var created = await Post<AppCreatePolicyPackResponse>(client, $"/api/orgs/{org}/policypacks",
            new AppCreatePolicyPackRequest { Name = "sec", DisplayName = "Security", Policies = new List<AppPolicy>() });
        Assert.Equal(1, created.Version);
        Assert.False(string.IsNullOrEmpty(created.UploadUri));

        using var complete = await client.PostAsJsonAsync($"/api/orgs/{org}/policypacks/sec/versions/1/complete", new { });
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);

        var pack = await client.GetFromJsonAsync<AppGetPolicyPackResponse>($"/api/orgs/{org}/policypacks/sec/versions/1");
        Assert.Equal("sec", pack!.Name);
        Assert.Equal(1, pack.Version);

        var list = await client.GetFromJsonAsync<AppListPolicyPacksResponse>($"/api/orgs/{org}/policypacks");
        Assert.Contains(list!.PolicyPacks, p => p.Name == "sec");

        using var delVersion = await client.DeleteAsync($"/api/orgs/{org}/policypacks/sec/versions/1");
        Assert.Equal(HttpStatusCode.NoContent, delVersion.StatusCode);
    }

    [Fact]
    public async Task PolicyPackConfigSchemaIsEmpty()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        await Post<AppCreatePolicyPackResponse>(client, $"/api/orgs/{org}/policypacks",
            new AppCreatePolicyPackRequest { Name = "sec", DisplayName = "Security", Policies = new List<AppPolicy>() });

        var schema = await client.GetFromJsonAsync<AppGetPolicyPackConfigSchemaResponse>(
            $"/api/orgs/{org}/policypacks/sec/versions/1/schema");

        Assert.NotNull(schema);
        Assert.Empty(schema!.ConfigSchema!);
    }

    [Fact]
    public async Task StackPolicyPacksAreEmpty()
    {
        using var client = app.CreateClient();

        var response = await client.GetFromJsonAsync<AppGetStackPolicyPacksResponse>(
            "/api/stacks/happypumi/webapp/dev/policypacks");

        Assert.NotNull(response);
        Assert.Empty(response!.RequiredPolicies!);
    }

    /// <summary>
    /// The full `pulumi policy publish` + `pulumi policy enable` handshake, then enforcement wiring: a
    /// published+enabled pack must come back as a required policy on update creation (where the engine reads
    /// it) and be downloadable. Mirrors what the CLI/engine do end-to-end (verified live against the runner).
    /// </summary>
    [Fact]
    public async Task PolicyPackPublishEnableAndEnforce()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        // 1) create — the CLI sends a semver version tag alongside the auto-incremented numeric version.
        var created = await Post<AppCreatePolicyPackResponse>(client, $"/api/orgs/{org}/policypacks",
            new AppCreatePolicyPackRequest
            {
                Name = "widget-policy", DisplayName = "Widget Policy", VersionTag = "1.0.0",
                Policies = new List<AppPolicy>(),
            });
        var version = created.Version;
        Assert.False(string.IsNullOrEmpty(created.UploadUri));

        // 2) upload the compressed pack to the pre-signed URL.
        var packBytes = new byte[] { 0x1f, 0x8b, 1, 2, 3, 4 }; // gzip-ish magic + payload
        using var upload = await client.PutAsync(
            $"/api/orgs/{org}/policypacks/widget-policy/versions/{version}/upload", new ByteArrayContent(packBytes));
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        // 3) complete — the CLI completes by the SEMVER TAG, not the numeric upload version.
        using var complete = await client.PostAsync(
            $"/api/orgs/{org}/policypacks/widget-policy/versions/1.0.0/complete", null);
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);

        // 4) enable it in the org's default policy group (pulumi policy enable → addPolicyPack object).
        using var enable = await client.PatchAsJsonAsync(
            $"/api/orgs/{org}/policygroups/default-policy-group",
            new { addPolicyPack = new { name = "widget-policy", versionTag = "1.0.0" } });
        Assert.Equal(HttpStatusCode.NoContent, enable.StatusCode);

        // 5) creating an update returns the pack as a required policy (the engine reads this to enforce it).
        using var _ = await client.PostAsJsonAsync($"/api/stacks/{org}/webapp",
            new AppCreateStackRequest { StackName = "dev" });
        var update = await Post<AppUpdateProgramResponse>(client, $"/api/stacks/{org}/webapp/dev/update",
            new AppUpdateProgramRequest());
        var required = Assert.Single(update.RequiredPolicies!);
        Assert.Equal("widget-policy", required.Name);
        Assert.Equal("1.0.0", required.VersionTag);
        Assert.Contains($"/policypacks/widget-policy/versions/{version}/download", required.PackLocation!);

        // 6) the stack policy-packs endpoint agrees.
        var stackPacks = await client.GetFromJsonAsync<AppGetStackPolicyPacksResponse>(
            $"/api/stacks/{org}/webapp/dev/policypacks");
        Assert.Contains(stackPacks!.RequiredPolicies!, p => p.Name == "widget-policy");

        // 7) the engine can download exactly the bytes we uploaded.
        using var download = await client.GetAsync(
            $"/api/orgs/{org}/policypacks/widget-policy/versions/{version}/download");
        download.EnsureSuccessStatusCode();
        Assert.Equal(packBytes, await download.Content.ReadAsByteArrayAsync());

        // 8) disabling removes it from enforcement.
        using var disable = await client.PatchAsJsonAsync(
            $"/api/orgs/{org}/policygroups/default-policy-group",
            new { removePolicyPack = new { name = "widget-policy" } });
        Assert.Equal(HttpStatusCode.NoContent, disable.StatusCode);
        var after = await client.GetFromJsonAsync<AppGetStackPolicyPacksResponse>(
            $"/api/stacks/{org}/webapp/dev/policypacks");
        Assert.Empty(after!.RequiredPolicies!);
    }

    [Fact]
    public async Task PolicyViolationEventsBecomePolicyFindings()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        // The engine streams a policy-violation event during an update; we capture it as a finding.
        var batch = new AppEngineEventBatch
        {
            Events = new List<AppEngineEvent>
            {
                new()
                {
                    PolicyEvent = new AppPolicyEvent
                    {
                        PolicyName = "randompet-min-length", PolicyPackName = "widget-policy",
                        PolicyPackVersion = "1", PolicyPackVersionTag = "1.0.0", EnforcementLevel = "advisory",
                        Message = "RandomPet length 2 is below the recommended minimum of 5.",
                        ResourceUrn = "urn:pulumi:dev::widget-template::random:index/randomPet:RandomPet::demo-id",
                    },
                },
                new() { StdoutEvent = new AppStdoutEngineEvent { Message = "noise" } }, // non-policy events ignored
            },
        };
        using var post = await client.PostAsJsonAsync(
            $"/api/stacks/{org}/widget-template/dev/update/u1/events/batch", batch);
        Assert.Equal(HttpStatusCode.NoContent, post.StatusCode);

        var res = await client.GetFromJsonAsync<ListPolicyViolationsV2Response>(
            $"/api/orgs/{org}/policyresults/violationsv2");
        var v = Assert.Single(res!.PolicyViolations);
        Assert.Equal("randompet-min-length", v.PolicyName);
        Assert.Equal("widget-policy", v.PolicyPack);
        Assert.Equal("1.0.0", v.PolicyPackTag);
        Assert.Equal("advisory", v.Level);
        Assert.Equal("widget-template", v.ProjectName);
        Assert.Equal("dev", v.StackName);
        Assert.Equal("random:index/randomPet:RandomPet", v.ResourceType);
        Assert.Equal("demo-id", v.ResourceName);
    }

    private static async Task<T> Post<T>(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
