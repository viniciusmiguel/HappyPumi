#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A workflow-runner (agent) pool and its access token. The token is minted on creation and presented by
/// the customer-managed workflow agent as <c>Authorization: token &lt;token&gt;</c> on its pool-scoped calls
/// (background-activities bootstrap, deployment poll). Key: Id.
/// </summary>
public sealed class AgentPoolRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = "";
    public string Token { get; set; } = default!;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
