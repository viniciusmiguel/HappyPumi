using System;
using System.Net.Http.Json;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Tests.Organizations;

/// <summary>
/// Component tests for the audit-log query &amp; export endpoints (org-admin PR2) against real Postgres. Events
/// are seeded through the real <see cref="IAuditLog"/> resolved from a request scope; the export config is
/// exercised end-to-end (get default → update → reflect → force/test → delete → reset). Unique org per test
/// for independence. Every endpoint must return 200/204, never 500.
/// </summary>
[Collection(HappyPumiCollection.Name)]
public sealed class AuditLogQueryTests(HappyPumiApp app)
{
    private static string NewOrg() => $"org-{Guid.NewGuid():N}";

    [Fact]
    public async Task V2ListReturnsSeededEventsAndFilters()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        Seed(org, a =>
        {
            a.Record(org, "stack.update", "Updated stack prod", "alice");
            a.Record(org, "member.added", "Added bob", "alice");
        });

        var all = await client.GetFromJsonAsync<ResponseAuditLogs>($"/api/orgs/{org}/auditlogs/v2");
        Assert.NotNull(all);
        Assert.Equal(2, all!.AuditLogEvents.Count);
        Assert.Null(all.ContinuationToken);

        var filtered = await client.GetFromJsonAsync<ResponseAuditLogs>(
            $"/api/orgs/{org}/auditlogs/v2?eventFilter=member");
        Assert.Single(filtered!.AuditLogEvents);
        Assert.Equal("member.added", filtered.AuditLogEvents[0].Event);
    }

    [Fact]
    public async Task ExportsReturnCsvWithHeaderAndRows()
    {
        using var client = app.CreateClient();
        var org = NewOrg();
        Seed(org, a => a.Record(org, "stack.update", "Updated, with comma", "alice"));

        foreach (var path in new[] { $"/api/orgs/{org}/auditlogs/export", $"/api/orgs/{org}/auditlogs/v2/export" })
        {
            var csv = await client.GetStringAsync(path);
            Assert.StartsWith("timestamp,event,actor,sourceIP,description", csv);
            Assert.Contains("stack.update", csv);
            Assert.Contains("\"Updated, with comma\"", csv); // comma-bearing field is quoted
        }
    }

    [Fact]
    public async Task ReaderKindIsDefault()
    {
        using var client = app.CreateClient();

        var kind = await client.GetStringAsync($"/api/orgs/{NewOrg()}/auditlogs/reader-kind");

        Assert.Equal("default", kind);
    }

    [Fact]
    public async Task ExportConfigRoundTrips()
    {
        using var client = app.CreateClient();
        var org = NewOrg();

        var initial = await GetConfig(client, org);
        Assert.False(initial!.Enabled);

        using var updated = await client.PostAsJsonAsync($"/api/orgs/{org}/auditlogs/export/config", new
        {
            newEnabled = true,
            newS3Configuration = new { iamRoleArn = "arn:aws:iam::1:role/p", s3BucketName = "audit", s3PathPrefix = "logs/" },
        });
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);

        var reflected = await GetConfig(client, org);
        Assert.True(reflected!.Enabled);
        Assert.Equal("audit", reflected.S3Config.S3BucketName);
        Assert.Equal("arn:aws:iam::1:role/p", reflected.S3Config.IamRoleArn);

        var forced = await PostResult(client, $"/api/orgs/{org}/auditlogs/export/config/force");
        Assert.Contains("Exported", forced!.Message);

        using var testResp = await client.PostAsJsonAsync($"/api/orgs/{org}/auditlogs/export/config/test",
            new { iamRoleArn = "arn:aws:iam::1:role/p", s3BucketName = "audit", s3PathPrefix = "logs/" });
        testResp.EnsureSuccessStatusCode();
        var tested = await testResp.Content.ReadFromJsonAsync<AuditLogExportResult>();
        Assert.Equal("Configuration valid", tested!.Message);

        using var deleted = await client.DeleteAsync($"/api/orgs/{org}/auditlogs/export/config");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.False((await GetConfig(client, org))!.Enabled);
    }

    [Fact]
    public async Task ForceOnDisabledConfigIsDescriptiveNot500()
    {
        using var client = app.CreateClient();

        var result = await PostResult(client, $"/api/orgs/{NewOrg()}/auditlogs/export/config/force");

        Assert.NotNull(result);
        Assert.Contains("not enabled", result!.Message);
    }

    private static async Task<OrganizationAuditLogExportSettings?> GetConfig(HttpClient client, string org)
        => await client.GetFromJsonAsync<OrganizationAuditLogExportSettings>($"/api/orgs/{org}/auditlogs/export/config");

    private static async Task<AuditLogExportResult?> PostResult(HttpClient client, string path)
    {
        using var resp = await client.PostAsJsonAsync(path, new { });
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<AuditLogExportResult>();
    }

    private void Seed(string org, Action<IAuditLog> seed)
    {
        using var scope = app.Services.CreateScope();
        seed(scope.ServiceProvider.GetRequiredService<IAuditLog>());
    }
}
