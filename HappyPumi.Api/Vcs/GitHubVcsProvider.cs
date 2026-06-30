#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;
using Microsoft.Extensions.Configuration;

namespace HappyPumi.Api.Vcs;

/// <summary>
/// Real-REST GitHub provider (github.com + GitHub Enterprise) behind the <see cref="IVcsProvider"/> seam
/// (ADR-0009). Wraps an injected <see cref="HttpClient"/>; the API base and auth token come from
/// <c>Vcs:GitHub:*</c> config, and a GitHub Enterprise integration's <see cref="StoredVcsIntegration.BaseUrl"/>
/// overrides the base (its REST API lives under <c>{baseUrl}/api/v3</c>). Config-gated: with no token
/// configured, access-status is "not configured" and the list calls return empty instead of throwing.
/// </summary>
public sealed class GitHubVcsProvider : IVcsProvider
{
    private const string DefaultApiBaseUrl = "https://api.github.com";
    private const string DefaultAppInstallUrl = "https://github.com/apps/happypumi/installations/new";

    private readonly HttpClient _http;
    private readonly string? _token;
    private readonly string _apiBaseUrl;

    public GitHubVcsProvider(HttpClient http, IConfiguration config)
    {
        _http = http;
        _token = config["Vcs:GitHub:Token"];
        _apiBaseUrl = (config["Vcs:GitHub:ApiBaseUrl"] ?? DefaultApiBaseUrl).TrimEnd('/');
        AppInstallUrl = config["Vcs:GitHub:AppInstallUrl"] ?? DefaultAppInstallUrl;
    }

    /// <summary>The GitHub App install/setup URL returned by <c>StartGitHubSetup</c> (from config).</summary>
    public string AppInstallUrl { get; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_token);

    public Task<bool> IsConfiguredAsync(CancellationToken ct = default) => Task.FromResult(IsConfigured);

    public async Task<IReadOnlyList<VcsRepo>> ListReposAsync(StoredVcsIntegration integration, CancellationToken ct = default)
    {
        var owner = integration.AccountName;
        var path = string.IsNullOrWhiteSpace(owner) ? "/user/repos?per_page=100" : $"/orgs/{owner}/repos?per_page=100";
        var repos = await GetJsonAsync<List<GitHubRepoDto>>(integration, path, ct);
        return repos is null ? Array.Empty<VcsRepo>() : repos.Select(ToRepo).ToArray();
    }

    public async Task<IReadOnlyList<VcsBranch>> ListBranchesAsync(StoredVcsIntegration integration, string repoId, CancellationToken ct = default)
    {
        // repoId is the GitHub "owner/repo" full name (what ListReposAsync stamps on VcsRepo.Id).
        var branches = await GetJsonAsync<List<GitHubBranchDto>>(integration, $"/repos/{repoId}/branches?per_page=100", ct);
        return branches is null ? Array.Empty<VcsBranch>() : branches.Select(ToBranch).ToArray();
    }

    public Task<IReadOnlyList<VcsRepo>> ListRepoDestinationsAsync(StoredVcsIntegration integration, CancellationToken ct = default)
        => ListReposAsync(integration, ct); // destinations = the repos the integration can target

    /// <summary>Lists a GitHub org's teams (<c>GET /orgs/{org}/teams</c>) for the GitHub-team endpoints.</summary>
    public async Task<IReadOnlyList<GitHubTeam>> ListOrganizationTeamsAsync(string ghOrg, StoredVcsIntegration? integration, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return Array.Empty<GitHubTeam>();
        var teams = await GetJsonAsync<List<GitHubTeamDto>>(integration, $"/orgs/{ghOrg}/teams?per_page=100", ct);
        return teams is null ? Array.Empty<GitHubTeam>() : teams.Select(ToTeam).ToArray();
    }

    private async Task<T?> GetJsonAsync<T>(StoredVcsIntegration? integration, string path, CancellationToken ct)
    {
        if (!IsConfigured)
            return default; // graceful degradation: no token → no call, empty result
        using var req = new HttpRequestMessage(HttpMethod.Get, ApiBaseFor(integration) + path);
        AddGitHubHeaders(req);
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            return default;
        return await resp.Content.ReadFromJsonAsync<T>(ct);
    }

    private void AddGitHubHeaders(HttpRequestMessage req)
    {
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        req.Headers.Accept.ParseAdd("application/vnd.github+json");
        req.Headers.UserAgent.ParseAdd("HappyPumi"); // GitHub rejects requests without a User-Agent
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    /// <summary>GitHub Enterprise serves REST under <c>{baseUrl}/api/v3</c>; github.com uses the config base.</summary>
    private string ApiBaseFor(StoredVcsIntegration? integration)
    {
        var baseUrl = integration?.BaseUrl;
        return string.IsNullOrWhiteSpace(baseUrl) ? _apiBaseUrl : baseUrl.TrimEnd('/') + "/api/v3";
    }

    private static VcsRepo ToRepo(GitHubRepoDto d) => new()
    {
        Id = d.FullName ?? d.Name ?? d.Id.ToString(),
        Name = d.Name ?? "",
        Owner = d.Owner?.Login ?? "",
    };

    private static VcsBranch ToBranch(GitHubBranchDto d) => new() { Name = d.Name ?? "", IsProtected = d.Protected };

    private static GitHubTeam ToTeam(GitHubTeamDto d) => new()
    {
        Id = d.Id,
        Name = d.Name ?? "",
        Slug = d.Slug ?? "",
        Description = d.Description ?? "",
        KnownToPulumi = false,
    };

    private sealed class GitHubRepoDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("full_name")] public string? FullName { get; set; }
        [JsonPropertyName("owner")] public GitHubOwnerDto? Owner { get; set; }
    }

    private sealed class GitHubOwnerDto
    {
        [JsonPropertyName("login")] public string? Login { get; set; }
    }

    private sealed class GitHubBranchDto
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("protected")] public bool Protected { get; set; }
    }

    private sealed class GitHubTeamDto
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }
}
