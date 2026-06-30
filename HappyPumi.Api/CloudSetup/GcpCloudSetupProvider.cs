#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using Microsoft.Extensions.Configuration;

namespace HappyPumi.Api.CloudSetup;

/// <summary>
/// Real-OAuth GCP cloud-setup provider (PR6). Builds the Google OAuth2 authorization URL, exchanges the
/// code at the Google token endpoint, and lists Cloud Resource Manager projects. Config-gated on
/// <c>CloudSetup:Gcp:{ClientId,ClientSecret,RedirectUri}</c>: unconfigured → empty URL / null credential /
/// empty accounts.
/// </summary>
public sealed class GcpCloudSetupProvider(HttpClient http, IConfiguration config) : ICloudSetupProvider
{
    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string ProjectsUrl = "https://cloudresourcemanager.googleapis.com/v1/projects";
    private const string DefaultScopes = "https://www.googleapis.com/auth/cloud-platform.read-only";

    private readonly string? _clientId = config["CloudSetup:Gcp:ClientId"];
    private readonly string? _clientSecret = config["CloudSetup:Gcp:ClientSecret"];
    private readonly string _redirectUri = config["CloudSetup:Gcp:RedirectUri"] ?? "";

    public string Key => "gcp";

    private bool IsConfigured => !string.IsNullOrWhiteSpace(_clientId);

    public string BuildAuthorizationUrl(string state, string? returnUrl)
    {
        if (!IsConfigured)
            return "";
        var redirect = string.IsNullOrWhiteSpace(returnUrl) ? _redirectUri : returnUrl;
        var query = $"client_id={Uri.EscapeDataString(_clientId!)}&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(redirect)}&access_type=offline" +
            $"&scope={Uri.EscapeDataString(DefaultScopes)}&state={Uri.EscapeDataString(state)}";
        return $"https://accounts.google.com/o/oauth2/v2/auth?{query}";
    }

    public Task<CloudCredential?> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        if (!IsConfigured)
            return Task.FromResult<CloudCredential?>(null);
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _clientId!,
            ["client_secret"] = _clientSecret ?? "",
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _redirectUri,
        };
        return CloudOAuthHttp.ExchangeFormAsync(http, TokenUrl, Key, form, ct);
    }

    public async Task<IReadOnlyList<CloudAccount>> ListAccountsAsync(CloudCredential cred, CancellationToken ct)
    {
        var list = await CloudOAuthHttp.GetWithBearerAsync<ProjectList>(http, ProjectsUrl, cred.AccessToken, ct);
        return list?.Projects is null ? Array.Empty<CloudAccount>() : list.Projects.Select(ToAccount).ToArray();
    }

    private static CloudAccount ToAccount(ProjectDto p) => new()
    {
        Id = p.ProjectId ?? "",
        Name = p.Name ?? p.ProjectId ?? "",
        Number = long.TryParse(p.ProjectNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null,
    };

    private sealed class ProjectList
    {
        [JsonPropertyName("projects")] public List<ProjectDto>? Projects { get; set; }
    }

    private sealed class ProjectDto
    {
        [JsonPropertyName("projectId")] public string? ProjectId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("projectNumber")] public string? ProjectNumber { get; set; }
    }
}
