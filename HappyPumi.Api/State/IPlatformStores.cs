#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Data.Entities;

namespace HappyPumi.Api.State;

/// <summary>IDP "services" (grouping of stacks/environments) per org. In-memory/Postgres (ADR-0005).</summary>
public interface IServiceStore
{
    IReadOnlyList<ServiceRow> List(string org);
    ServiceRow? Create(string org, string name, string displayName, string description);
    bool Delete(string org, string name);
}

/// <summary>Connected cloud accounts (Insights resource scanning) per org.</summary>
public interface ICloudAccountStore
{
    IReadOnlyList<CloudAccountRow> List(string org);
    CloudAccountRow? Create(string org, string name, string provider, string description);
    bool Delete(string org, string name);
}

/// <summary>Connected version-control accounts (ADR-0009) per org.</summary>
public interface IVcsConnectionStore
{
    IReadOnlyList<VcsConnectionRow> List(string org);
    VcsConnectionRow? Create(string org, string name, string kind);
    bool Delete(string org, string name);
}

/// <summary>Trusted OIDC issuers (SSO/identity providers) per org.</summary>
public interface IOidcIssuerStore
{
    IReadOnlyList<OidcIssuerRow> List(string org);
    OidcIssuerRow? Create(string org, string name, string url);
    bool Delete(string org, string name);
}

/// <summary>Approval rules requiring sign-off before updates to matching stacks, per org.</summary>
public interface IApprovalRuleStore
{
    IReadOnlyList<ApprovalRuleRow> List(string org);
    ApprovalRuleRow? Create(string org, string name, string stackPattern, int requiredApprovals);
    bool Delete(string org, string name);
}
