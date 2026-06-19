#nullable enable

using System.Collections.Generic;

namespace HappyPumi.Api.Esc.Providers.Logins;

// fn::open::aws-login lives in Aws/AwsLoginProvider.cs — it supports OIDC federation (assume-role-with-web-
// identity) in addition to the static-credential mode the providers below use.

/// <summary>fn::open::azure-login — Azure service-principal credentials.</summary>
public sealed class AzureLoginProvider : StaticLoginProvider
{
    public override string Name => "azure-login";
    protected override string Cloud => "Azure";
    protected override IReadOnlyList<LoginField> Fields => new[]
    {
        new LoginField("clientId", Secret: false, Required: true),
        new LoginField("clientSecret", Secret: true, Required: true),
        new LoginField("tenantId", Secret: false, Required: true),
        new LoginField("subscriptionId", Secret: false, Required: false),
    };
}

/// <summary>fn::open::gcp-login — Google Cloud credentials (access token).</summary>
public sealed class GcpLoginProvider : StaticLoginProvider
{
    public override string Name => "gcp-login";
    protected override string Cloud => "Google Cloud";
    protected override IReadOnlyList<LoginField> Fields => new[]
    {
        new LoginField("accessToken", Secret: true, Required: true),
        new LoginField("project", Secret: false, Required: false),
    };
}

/// <summary>fn::open::vault-login — HashiCorp Vault address + token.</summary>
public sealed class VaultLoginProvider : StaticLoginProvider
{
    public override string Name => "vault-login";
    protected override string Cloud => "HashiCorp Vault";
    protected override IReadOnlyList<LoginField> Fields => new[]
    {
        new LoginField("address", Secret: false, Required: true),
        new LoginField("token", Secret: true, Required: true),
    };
}
