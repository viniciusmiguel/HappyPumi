#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using Microsoft.Extensions.Configuration;

namespace HappyPumi.Api.CloudSetup;

/// <summary>Result of starting an AWS SSO device-authorization flow: a session id, a verification URL, and a user code.</summary>
public sealed record AwsSsoInitiation(string SessionId, string Url, string UserCode);

/// <summary>
/// AWS SSO cloud-setup provider (PR6). AWS uses the OIDC device-authorization flow rather than a redirect
/// code grant, so <see cref="InitiateSso"/> is the primary entry point (config-gated on
/// <c>CloudSetup:AwsSso:{StartUrl,Region,ClientId}</c>, degrading to a deterministic verification URL +
/// generated user code when unconfigured). The <see cref="ICloudSetupProvider"/> redirect methods are
/// implemented for parity but return empty/null when no SSO client is configured.
/// </summary>
public sealed class AwsCloudSetupProvider(HttpClient http, IConfiguration config) : ICloudSetupProvider
{
    private const string DefaultStartUrl = "https://device.sso.amazonaws.com/";

    private readonly string? _startUrl = config["CloudSetup:AwsSso:StartUrl"];
    private readonly string _region = config["CloudSetup:AwsSso:Region"] ?? "us-east-1";
    private readonly string? _clientId = config["CloudSetup:AwsSso:ClientId"];

    public string Key => "aws";

    private bool IsConfigured => !string.IsNullOrWhiteSpace(_clientId) || !string.IsNullOrWhiteSpace(_startUrl);

    /// <summary>
    /// Starts the SSO device flow. The verification URL prefers the request <paramref name="startUrl"/>, then
    /// configured <c>CloudSetup:AwsSso:StartUrl</c>, then a default — so it always returns a usable response.
    /// </summary>
    public AwsSsoInitiation InitiateSso(string? startUrl, string? region)
    {
        var baseUrl = FirstNonEmpty(startUrl, _startUrl, DefaultStartUrl).TrimEnd('/');
        var userCode = GenerateUserCode();
        var url = $"{baseUrl}/?user_code={Uri.EscapeDataString(userCode)}";
        return new AwsSsoInitiation(Guid.NewGuid().ToString("N"), url, userCode);
    }

    public string BuildAuthorizationUrl(string state, string? returnUrl)
        => IsConfigured ? InitiateSso(returnUrl, _region).Url : "";

    public Task<CloudCredential?> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        if (!IsConfigured)
            return Task.FromResult<CloudCredential?>(null);
        var form = new Dictionary<string, string>
        {
            ["clientId"] = _clientId ?? "",
            ["grantType"] = "urn:ietf:params:oauth:grant-type:device_code",
            ["deviceCode"] = code,
        };
        return CloudOAuthHttp.ExchangeFormAsync(http, $"https://oidc.{_region}.amazonaws.com/token", Key, form, ct);
    }

    // AWS SSO account listing requires the portal API with a bearer-token header shape we don't model here;
    // connected accounts are surfaced from the store, so live listing degrades to empty.
    public Task<IReadOnlyList<CloudAccount>> ListAccountsAsync(CloudCredential cred, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<CloudAccount>>(Array.Empty<CloudAccount>());

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v))
                return v;
        return DefaultStartUrl;
    }

    /// <summary>Generates an AWS-SSO-style "XXXX-XXXX" user code from an unambiguous alphabet.</summary>
    private static string GenerateUserCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> code = stackalloc char[9];
        for (var i = 0; i < 9; i++)
            code[i] = i == 4 ? '-' : alphabet[Random.Shared.Next(alphabet.Length)];
        return new string(code);
    }
}
