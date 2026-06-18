#nullable enable

namespace HappyPumi.Api.Auth;

/// <summary>Authorization policy names (ADR-0007). Referenced by endpoints via <c>Policies(...)</c>.</summary>
public static class AuthPolicies
{
    /// <summary>Requires the caller to hold the org <c>admin</c> role; gates org-management endpoints.</summary>
    public const string OrgAdmin = "OrgAdmin";
}
