using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Endpoints.Organizations;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the change-request core (change-requests PR2) against the real Postgres-backed stores:
/// a draft creates a CR (status draft) that appears in list/get; update/submit/comment/apply each move the
/// status and append timeline events; apply commits a new env revision. Approve/unapprove operate on the CR
/// (with separation-of-duties). The polymorphic CR/event responses are deserialized with
/// <see cref="ChangeGateJson.Options"/> (the default STJ binder rejects the discriminator collision).
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class ChangeRequestTests(HappyPumiApp app)
{
    private const string Project = "cr-proj";

    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task DraftCreatesChangeRequestWithLifecycleEvents()
    {
        using var client = app.CreateAuthedClient("role:admin:alice");
        var org = NewOrg();
        var env = await CreateEnv(client, org);
        var id = await CreateDraft(client, org, env, "values:\n  region: us-west-2\n");

        var list = await GetList(client, $"/api/change-requests/{org}");
        Assert.Contains(list.ChangeRequests, c => c.Id == id);

        var cr = await GetCr(client, $"/api/change-requests/{org}/{id}");
        Assert.Equal("draft", cr.Status);
        Assert.IsType<TargetEntityEnvironment>(cr.Entity);

        await Patch(client, $"/api/change-requests/{org}/{id}", new UpdateChangeRequestRequest { Description = "ship it" });
        await Post(client, $"/api/change-requests/{org}/{id}/submit", new SubmitChangeRequestRequest { Description = "ready" });
        await Post(client, $"/api/change-requests/{org}/{id}/comments", new AddChangeRequestCommentRequest { Comment = "lgtm" });

        Assert.Equal("submitted", (await GetCr(client, $"/api/change-requests/{org}/{id}")).Status);
        // The resolver drops the discriminator property on read, so assert by the concrete polymorphic subtype.
        var events = await GetEvents(client, org, id);
        Assert.Contains(events, e => e is ChangeRequestDescriptionUpdatedEvent);
        Assert.Contains(events, e => e is ChangeRequestStatusChangedEvent);
        Assert.Contains(events, e => e is ChangeRequestCommentedEvent);

        var apply = await ApplyCr(client, org, id);
        Assert.False(string.IsNullOrEmpty(apply.EntityUrl));
        Assert.Equal($"/{org}/{Project}/{env}", apply.EntityUrl);

        var applied = await GetCr(client, $"/api/change-requests/{org}/{id}");
        Assert.Equal("applied", applied.Status);
        Assert.Equal(2, applied.LatestRevisionNumber);
        Assert.Contains(await GetEvents(client, org, id), e => e is ChangeRequestRevisionAddedEvent);

        var revisions = await client.GetFromJsonAsync<List<EnvironmentRevision>>(
            $"/api/esc/environments/{org}/{Project}/{env}/versions");
        Assert.True(revisions!.Any(r => r.Number == 2));
    }

    [Fact]
    public async Task ApproveRecordsApprovalAndEnforcesSeparationOfDuties()
    {
        using var creator = app.CreateAuthedClient("role:admin:alice");
        using var approver = app.CreateAuthedClient("role:admin:bob");
        var org = NewOrg();
        var env = await CreateEnv(creator, org);
        var id = await CreateDraft(creator, org, env, "values:\n  region: eu-west-1\n");

        using var ok = await approver.PostAsJsonAsync($"/api/change-requests/{org}/{id}/approve", new { });
        ok.EnsureSuccessStatusCode();
        var body = await ok.Content.ReadFromJsonAsync<ApprovalCount>();
        Assert.Equal(1, body!.Approvals);
        Assert.Contains(await GetEvents(approver, org, id), e => e is ChangeRequestApprovedEvent);

        // Separation of duties: the creator cannot approve their own change request.
        using var self = await creator.PostAsJsonAsync($"/api/change-requests/{org}/{id}/approve", new { });
        Assert.Equal(HttpStatusCode.Forbidden, self.StatusCode);

        using var undo = await approver.DeleteAsync($"/api/change-requests/{org}/{id}/approve");
        Assert.Equal(HttpStatusCode.NoContent, undo.StatusCode);
        Assert.Contains(await GetEvents(approver, org, id), e => e is ChangeRequestUnapprovedEvent);
    }

    [Fact]
    public async Task UnknownChangeRequestIsNotFound()
    {
        using var client = app.CreateAuthedClient();
        var org = NewOrg();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/change-requests/{org}/ghost")).StatusCode);
    }

    private sealed record ApprovalCount(int Approvals);

    private static async Task<string> CreateEnv(HttpClient client, string org)
    {
        var name = $"env-{Guid.NewGuid():N}";
        using var res = await client.PostAsJsonAsync($"/api/esc/environments/{org}",
            new CreateEnvironmentRequest { Project = Project, Name = name });
        res.EnsureSuccessStatusCode();
        return name;
    }

    private static async Task<string> CreateDraft(HttpClient client, string org, string env, string yaml)
    {
        using var res = await client.PostAsync($"/api/esc/environments/{org}/{Project}/{env}/drafts",
            new StringContent(yaml, Encoding.UTF8, "application/x-yaml"));
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<ChangeRequestRef>();
        return created!.ChangeRequestId!;
    }

    private static async Task Patch(HttpClient client, string url, object body)
    {
        using var res = await client.PatchAsync(url, JsonContent(body));
        res.EnsureSuccessStatusCode();
    }

    private static async Task Post(HttpClient client, string url, object body)
    {
        using var res = await client.PostAsync(url, JsonContent(body));
        res.EnsureSuccessStatusCode();
    }

    private static async Task<ChangeRequestApplyResult> ApplyCr(HttpClient client, string org, string id)
    {
        using var res = await client.PostAsync($"/api/change-requests/{org}/{id}/apply", JsonContent(new { }));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ChangeRequestApplyResult>())!;
    }

    private static StringContent JsonContent(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static async Task<GetChangeRequestResponse> GetCr(HttpClient client, string url)
        => Deserialize<GetChangeRequestResponse>(await client.GetStringAsync(url));

    private static async Task<ListChangeRequestsResponse> GetList(HttpClient client, string url)
        => Deserialize<ListChangeRequestsResponse>(await client.GetStringAsync(url));

    private static async Task<List<ChangeRequestEvent>> GetEvents(HttpClient client, string org, string id)
    {
        var raw = await client.GetStringAsync($"/api/change-requests/{org}/{id}/events");
        return Deserialize<ListChangeRequestEventsResponse>(raw).Events;
    }

    private static T Deserialize<T>(string raw) => JsonSerializer.Deserialize<T>(raw, ChangeGateJson.Options)!;
}
