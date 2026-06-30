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
/// Real-REST Azure DevOps provider behind the <see cref="IVcsProvider"/> seam (ADR-0009). Wraps an injected
/// <see cref="HttpClient"/>; the OAuth app comes from <c>Vcs:AzureDevOps:*</c> config and the bearer for REST
/// calls is the per-integration access token persisted on OAuth completion
/// (<see cref="StoredVcsIntegration.Credential"/>). Config-gated: with no OAuth client configured the
/// authorization URL is empty, and with no access token the list calls return empty instead of throwing.
/// </summary>
public sealed class AzureDevOpsVcsProvider : IVcsProvider
{
    private const string DefaultApiBaseUrl = "https://dev.azure.com";
    private const string VsspsBaseUrl = "https://app.vssps.visualstudio.com";
    private const string ApiVersion = "api-version=7.1";
    private const string DefaultScopes = "vso.code vso.project";
    private const string JwtBearerGrant = "urn:ietf:params:oauth:grant-type:jwt-bearer";
    private const string JwtBearerAssertion = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

    private readonly HttpClient _http;
    private readonly string? _clientId;
    private readonly string? _clientSecret;
    private readonly string _redirectUri;
    private readonly string _scopes;
    private readonly string _apiBaseUrl;

    public AzureDevOpsVcsProvider(HttpClient http, IConfiguration config)
    {
        _http = http;
        _clientId = config["Vcs:AzureDevOps:ClientId"];
        _clientSecret = config["Vcs:AzureDevOps:ClientSecret"];
        _redirectUri = config["Vcs:AzureDevOps:RedirectUri"] ?? "";
        _scopes = config["Vcs:AzureDevOps:Scopes"] ?? DefaultScopes;
        _apiBaseUrl = (config["Vcs:AzureDevOps:ApiBaseUrl"] ?? DefaultApiBaseUrl).TrimEnd('/');
    }

    /// <summary>True once the OAuth client is configured; gates the authorize/exchange flow.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_clientId);

    public Task<bool> IsConfiguredAsync(CancellationToken ct = default) => Task.FromResult(IsConfigured);

    /// <summary>Builds the Azure DevOps OAuth authorize URL (empty when the client is unconfigured).</summary>
    public string BuildAuthorizationUrl(string state)
    {
        if (!IsConfigured)
            return "";
        var query = $"client_id={Uri.EscapeDataString(_clientId!)}&response_type=Assertion" +
            $"&state={Uri.EscapeDataString(state)}&scope={Uri.EscapeDataString(_scopes)}" +
            $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}";
        return $"{VsspsBaseUrl}/oauth2/authorize?{query}";
    }

    /// <summary>Exchanges an authorization code for an access token (POST token endpoint); null when unconfigured.</summary>
    public async Task<string?> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return null;
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{VsspsBaseUrl}/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_assertion_type"] = JwtBearerAssertion,
                ["client_assertion"] = _clientSecret ?? "",
                ["grant_type"] = JwtBearerGrant,
                ["assertion"] = code,
                ["redirect_uri"] = _redirectUri,
            }),
        };
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            return null;
        var token = await resp.Content.ReadFromJsonAsync<AdoTokenDto>(ct);
        return token?.AccessToken;
    }

    public async Task<IReadOnlyList<VcsRepo>> ListReposAsync(StoredVcsIntegration integration, CancellationToken ct = default)
    {
        var path = $"/{integration.AccountName}/{integration.AzureProject}/_apis/git/repositories?{ApiVersion}";
        var repos = await GetJsonAsync<AdoList<AdoRepoDto>>(integration.Credential, _apiBaseUrl + path, ct);
        return repos?.Value is null ? Array.Empty<VcsRepo>() : repos.Value.Select(ToRepo).ToArray();
    }

    public async Task<IReadOnlyList<VcsBranch>> ListBranchesAsync(StoredVcsIntegration integration, string repoId, CancellationToken ct = default)
    {
        var path = $"/{integration.AccountName}/{integration.AzureProject}/_apis/git/repositories/{repoId}/refs?{ApiVersion}";
        var refs = await GetJsonAsync<AdoList<AdoRefDto>>(integration.Credential, _apiBaseUrl + path, ct);
        return refs?.Value is null ? Array.Empty<VcsBranch>() : refs.Value.Select(ToBranch).ToArray();
    }

    public Task<IReadOnlyList<VcsRepo>> ListRepoDestinationsAsync(StoredVcsIntegration integration, CancellationToken ct = default)
        => ListReposAsync(integration, ct);

    /// <summary>Lists the Azure DevOps organizations the access token's member can reach (profile → accounts).</summary>
    public async Task<IReadOnlyList<AzureDevOpsOrganization>> ListOrganizationsAsync(string? accessToken, CancellationToken ct = default)
    {
        var profile = await GetJsonAsync<AdoProfileDto>(accessToken, $"{VsspsBaseUrl}/_apis/profile/profiles/me?{ApiVersion}", ct);
        if (profile?.Id is null)
            return Array.Empty<AzureDevOpsOrganization>();
        var accounts = await GetJsonAsync<AdoList<AdoAccountDto>>(
            accessToken, $"{VsspsBaseUrl}/_apis/accounts?memberId={profile.Id}&{ApiVersion}", ct);
        return accounts?.Value is null ? Array.Empty<AzureDevOpsOrganization>() : accounts.Value.Select(ToOrg).ToArray();
    }

    /// <summary>Lists the projects in one Azure DevOps organization (empty when no token).</summary>
    public async Task<IReadOnlyList<AzureDevOpsProject>> ListProjectsAsync(string adoOrg, string? accessToken, CancellationToken ct = default)
    {
        var projects = await GetJsonAsync<AdoList<AdoProjectDto>>(accessToken, $"{_apiBaseUrl}/{adoOrg}/_apis/projects?{ApiVersion}", ct);
        return projects?.Value is null ? Array.Empty<AzureDevOpsProject>() : projects.Value.Select(ToProject).ToArray();
    }

    private async Task<T?> GetJsonAsync<T>(string? accessToken, string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return default; // graceful degradation: no token → no call, empty result
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            return default;
        return await resp.Content.ReadFromJsonAsync<T>(ct);
    }

    private static VcsRepo ToRepo(AdoRepoDto d) => new()
    {
        Id = d.Id ?? d.Name ?? "",
        Name = d.Name ?? "",
        Owner = d.Project?.Name ?? "",
    };

    private static VcsBranch ToBranch(AdoRefDto d) => new()
    {
        Name = StripRefPrefix(d.Name),
        IsProtected = d.IsLocked,
    };

    private static string StripRefPrefix(string? name)
        => name is null ? "" : name.StartsWith("refs/heads/", StringComparison.Ordinal) ? name["refs/heads/".Length..] : name;

    private static AzureDevOpsOrganization ToOrg(AdoAccountDto d) => new()
    {
        Id = d.AccountId,
        Name = d.AccountName ?? "",
        AccountUrl = string.IsNullOrWhiteSpace(d.AccountName) ? null : $"{DefaultApiBaseUrl}/{d.AccountName}",
        HasRequiredPermissions = true,
    };

    private static AzureDevOpsProject ToProject(AdoProjectDto d) => new() { Id = d.Id ?? "", Name = d.Name ?? "" };

    private sealed class AdoList<T>
    {
        [JsonPropertyName("value")] public List<T>? Value { get; set; }
    }

    private sealed class AdoTokenDto
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    }

    private sealed class AdoRepoDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("project")] public AdoProjectDto? Project { get; set; }
    }

    private sealed class AdoRefDto
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("isLocked")] public bool IsLocked { get; set; }
    }

    private sealed class AdoProfileDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
    }

    private sealed class AdoAccountDto
    {
        [JsonPropertyName("accountId")] public string? AccountId { get; set; }
        [JsonPropertyName("accountName")] public string? AccountName { get; set; }
    }

    private sealed class AdoProjectDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
