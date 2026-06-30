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

    /// <summary>Fetch a single issuer by its opaque GUID id; null if not found in the org.</summary>
    OidcIssuerRow? Get(string org, string id);

    /// <summary>Registers an issuer with only a name/url (assigns an Id). Null if the name already exists.</summary>
    OidcIssuerRow? Create(string org, string name, string url);

    /// <summary>Registers an issuer with thumbprints/max-expiration (assigns an Id). Null if the name already exists.</summary>
    OidcIssuerRow? Create(string org, string name, string url, List<string>? thumbprints, long? maxExpiration);

    /// <summary>Patches the supplied (non-null) fields by id and bumps Modified; null if the issuer is missing.</summary>
    OidcIssuerRow? Update(string org, string id, string? name, long? maxExpiration, List<string>? thumbprints);

    /// <summary>Replaces an issuer's thumbprints (regenerate flow) and bumps Modified; null if missing.</summary>
    OidcIssuerRow? SetThumbprints(string org, string id, IReadOnlyList<string> thumbprints);

    bool Delete(string org, string name);

    /// <summary>Removes an issuer by its opaque GUID id; false if not found.</summary>
    bool DeleteById(string org, string id);
}

/// <summary>Approval rules requiring sign-off before updates to matching stacks, per org.</summary>
public interface IApprovalRuleStore
{
    IReadOnlyList<ApprovalRuleRow> List(string org);
    ApprovalRuleRow? Create(string org, string name, string stackPattern, int requiredApprovals);
    bool Delete(string org, string name);
}
