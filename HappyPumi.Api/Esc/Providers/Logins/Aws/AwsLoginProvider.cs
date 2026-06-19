#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc;
using HappyPumi.Api.Esc.Oidc;

namespace HappyPumi.Api.Esc.Providers.Logins.Aws;

/// <summary>
/// fn::open::aws-login — AWS credentials at open time, in either of two modes:
/// <list type="bullet">
/// <item><b>OIDC federation</b> (preferred): when an <c>oidc</c> block is present, mint a Pulumi web-identity
/// token and exchange it via STS <c>AssumeRoleWithWebIdentity</c> for short-lived credentials. No long-lived
/// secret is stored in the environment.</item>
/// <item><b>Static</b>: pass through access keys supplied in the definition (see <see cref="StaticLoginProvider"/>).</item>
/// </list>
/// </summary>
public sealed class AwsLoginProvider(IEscOidcIssuer issuer, IAwsStsExchanger sts) : StaticLoginProvider
{
    private const string DefaultAudience = "sts.amazonaws.com";
    private const string DefaultSessionName = "pulumi-esc";
    private const string DefaultSubject = "pulumi:environments";

    public override string Name => "aws-login";
    protected override string Cloud => "AWS";

    public override string Description =>
        "Provides AWS credentials at open time via OIDC federation (assume-role-with-web-identity) or static access keys.";

    // Required only in static mode; OIDC federation branches before the base enforces these (see OpenAsync).
    protected override IReadOnlyList<LoginField> Fields => new[]
    {
        new LoginField("accessKeyId", Secret: true, Required: true),
        new LoginField("secretAccessKey", Secret: true, Required: true),
        new LoginField("sessionToken", Secret: true, Required: false),
        new LoginField("region", Secret: false, Required: false),
    };

    public override EscSchemaSchema Inputs
    {
        get
        {
            var schema = base.Inputs;
            schema.Properties!["oidc"] = OidcInputSchema;
            return schema;
        }
    }

    public override async Task<object?> OpenAsync(IReadOnlyDictionary<string, object?> inputs, CancellationToken ct)
    {
        if (inputs.GetValueOrDefault("oidc") is Dictionary<string, object?> oidc)
            return await FederateAsync(oidc, ct);
        return await base.OpenAsync(inputs, ct);
    }

    // Mint a web-identity token for the configured AWS audience/subject, then assume the role with it.
    private async Task<object?> FederateAsync(IReadOnlyDictionary<string, object?> oidc, CancellationToken ct)
    {
        var roleArn = EscProviderInputs.Require<string>(oidc, Name, "roleArn");
        var token = issuer.IssueToken(new EscOidcTokenRequest(
            Audience: AsString(oidc, "audience") ?? DefaultAudience,
            Subject: AsString(oidc, "subject") ?? DefaultSubject));

        var creds = await sts.AssumeRoleWithWebIdentityAsync(new AwsWebIdentityRequest(
            RoleArn: roleArn,
            SessionName: AsString(oidc, "sessionName") ?? DefaultSessionName,
            WebIdentityToken: token,
            Region: AsString(oidc, "region"),
            DurationSeconds: AsInt(oidc, "durationSeconds")), ct);

        var output = new Dictionary<string, object?>
        {
            ["accessKeyId"] = EscProviderInputs.Secret(creds.AccessKeyId),
            ["secretAccessKey"] = EscProviderInputs.Secret(creds.SecretAccessKey),
            ["sessionToken"] = EscProviderInputs.Secret(creds.SessionToken),
        };
        if (AsString(oidc, "region") is { } region)
            output["region"] = region;
        return output;
    }

    private static string? AsString(IReadOnlyDictionary<string, object?> map, string key)
        => map.GetValueOrDefault(key) as string;

    private static int? AsInt(IReadOnlyDictionary<string, object?> map, string key)
        => map.GetValueOrDefault(key) switch
        {
            null => null,
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) => v,
            var other => throw new ArgumentException($"aws-login '{key}' must be an integer; got {EscProviderInputs.Describe(other)}."),
        };

    private static EscSchemaSchema OidcInputSchema => new()
    {
        Type = "object",
        Description = "OIDC federation: assume an AWS role from a Pulumi-issued web-identity token.",
        Required = new List<string> { "roleArn" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["roleArn"] = new() { Type = "string", Description = "The IAM role ARN to assume." },
            ["sessionName"] = new() { Type = "string", Description = $"STS role session name (default '{DefaultSessionName}')." },
            ["audience"] = new() { Type = "string", Description = $"Token audience (default '{DefaultAudience}')." },
            ["subject"] = new() { Type = "string", Description = "Token subject claim for the role's trust policy." },
            ["region"] = new() { Type = "string", Description = "AWS region for the STS endpoint." },
            ["durationSeconds"] = new() { Type = "integer", Description = "Credential lifetime in seconds (default 3600)." },
        },
    };
}
