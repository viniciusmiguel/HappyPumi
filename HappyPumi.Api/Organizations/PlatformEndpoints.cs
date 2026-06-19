#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using HappyPumi.Api.Auth;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

// Console-facing CRUD for the platform/management surfaces that have no clean spec list endpoint
// (Insights cloud accounts, VCS connections, identity providers, approval rules). New routes under
// /api/orgs/{org}/... — NOT /console/ (which the dev MockConsole shadows). Each create records an audit event.

internal static class Actor
{
    public static string Of(Microsoft.AspNetCore.Http.HttpContext ctx)
        => RequestActor.From(ctx.User)?.Name ?? "happypumi";
}

/// <summary>GET /api/orgs/{org}/cloud-accounts — connected cloud accounts for Insights.</summary>
public sealed class ListCloudAccountsEndpoint(ICloudAccountStore accounts) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/orgs/{orgName}/cloud-accounts");
        AllowAnonymous();
        Description(b => b.WithTags("Organizations").WithName("ListCloudAccounts"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var org = Route<string>("orgName")!;
        var list = accounts.List(org).Select(a => new
        {
            name = a.Name, provider = a.Provider, description = a.Description, created = a.Created,
        });
        await Send.OkAsync(new { accounts = list }, ct);
    }
}

/// <summary>POST /api/orgs/{org}/cloud-accounts — connect a cloud account.</summary>
public sealed class CreateCloudAccountEndpoint(ICloudAccountStore accounts, IAuditLog audit) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/orgs/{orgName}/cloud-accounts");
        AllowAnonymous();
        Description(b => b.WithTags("Organizations").WithName("CreateCloudAccount"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var org = Route<string>("orgName")!;
        var body = await PlatformBody.ReadAsync(HttpContext, ct);
        var name = body.Str("name");
        if (string.IsNullOrWhiteSpace(name)) { await Send.ErrorsAsync(400, ct); return; }
        var row = accounts.Create(org, name, body.Str("provider") ?? "aws", body.Str("description") ?? "");
        if (row is null) { await Send.ErrorsAsync(409, ct); return; }
        audit.Record(org, "cloudAccount.create", $"Connected cloud account '{name}'", Actor.Of(HttpContext));
        await Send.OkAsync(new { name = row.Name, provider = row.Provider }, ct);
    }
}

/// <summary>GET /api/orgs/{org}/vcs-connections — connected version-control accounts (ADR-0009).</summary>
public sealed class ListVcsConnectionsEndpoint(IVcsConnectionStore vcs) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/orgs/{orgName}/vcs-connections");
        AllowAnonymous();
        Description(b => b.WithTags("Organizations").WithName("ListVcsConnections"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var org = Route<string>("orgName")!;
        var list = vcs.List(org).Select(c => new { name = c.Name, kind = c.Kind, created = c.Created });
        await Send.OkAsync(new { connections = list }, ct);
    }
}

/// <summary>POST /api/orgs/{org}/vcs-connections — connect a version-control account.</summary>
public sealed class CreateVcsConnectionEndpoint(IVcsConnectionStore vcs, IAuditLog audit) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/orgs/{orgName}/vcs-connections");
        AllowAnonymous();
        Description(b => b.WithTags("Organizations").WithName("CreateVcsConnection"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var org = Route<string>("orgName")!;
        var body = await PlatformBody.ReadAsync(HttpContext, ct);
        var name = body.Str("name");
        if (string.IsNullOrWhiteSpace(name)) { await Send.ErrorsAsync(400, ct); return; }
        var row = vcs.Create(org, name, body.Str("kind") ?? "github");
        if (row is null) { await Send.ErrorsAsync(409, ct); return; }
        audit.Record(org, "vcsConnection.create", $"Connected {row.Kind} account '{name}'", Actor.Of(HttpContext));
        await Send.OkAsync(new { name = row.Name, kind = row.Kind }, ct);
    }
}

/// <summary>GET /api/orgs/{org}/oidc-issuers — trusted OIDC issuers (identity providers).</summary>
public sealed class ListOidcIssuersEndpoint(IOidcIssuerStore issuers) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/orgs/{orgName}/oidc-issuers");
        AllowAnonymous();
        Description(b => b.WithTags("Organizations").WithName("ListOidcIssuers"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var org = Route<string>("orgName")!;
        var list = issuers.List(org).Select(i => new { name = i.Name, url = i.Url, created = i.Created });
        await Send.OkAsync(new { issuers = list }, ct);
    }
}

/// <summary>POST /api/orgs/{org}/oidc-issuers — register an OIDC issuer.</summary>
public sealed class CreateOidcIssuerEndpoint(IOidcIssuerStore issuers, IAuditLog audit) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/orgs/{orgName}/oidc-issuers");
        AllowAnonymous();
        Description(b => b.WithTags("Organizations").WithName("CreateOidcIssuer"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var org = Route<string>("orgName")!;
        var body = await PlatformBody.ReadAsync(HttpContext, ct);
        var name = body.Str("name");
        var url = body.Str("url");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) { await Send.ErrorsAsync(400, ct); return; }
        var row = issuers.Create(org, name, url);
        if (row is null) { await Send.ErrorsAsync(409, ct); return; }
        audit.Record(org, "identityProvider.create", $"Added identity provider '{name}'", Actor.Of(HttpContext));
        await Send.OkAsync(new { name = row.Name, url = row.Url }, ct);
    }
}

/// <summary>GET /api/orgs/{org}/approval-rules — approval rules gating updates.</summary>
public sealed class ListApprovalRulesEndpoint(IApprovalRuleStore rules) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/orgs/{orgName}/approval-rules");
        AllowAnonymous();
        Description(b => b.WithTags("Organizations").WithName("ListApprovalRules"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var org = Route<string>("orgName")!;
        var list = rules.List(org).Select(r => new
        {
            name = r.Name, stackPattern = r.StackPattern, requiredApprovals = r.RequiredApprovals,
            enabled = r.Enabled, created = r.Created,
        });
        await Send.OkAsync(new { rules = list }, ct);
    }
}

/// <summary>POST /api/orgs/{org}/approval-rules — create an approval rule.</summary>
public sealed class CreateApprovalRuleEndpoint(IApprovalRuleStore rules, IAuditLog audit) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/api/orgs/{orgName}/approval-rules");
        AllowAnonymous();
        Description(b => b.WithTags("Organizations").WithName("CreateApprovalRule"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var org = Route<string>("orgName")!;
        var body = await PlatformBody.ReadAsync(HttpContext, ct);
        var name = body.Str("name");
        if (string.IsNullOrWhiteSpace(name)) { await Send.ErrorsAsync(400, ct); return; }
        var row = rules.Create(org, name, body.Str("stackPattern") ?? "*", body.Int("requiredApprovals", 1));
        if (row is null) { await Send.ErrorsAsync(409, ct); return; }
        audit.Record(org, "approvalRule.create", $"Created approval rule '{name}'", Actor.Of(HttpContext));
        await Send.OkAsync(new { name = row.Name, stackPattern = row.StackPattern }, ct);
    }
}
