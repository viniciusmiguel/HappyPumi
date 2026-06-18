#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HappyPumi.Api.Contracts;

/// <summary>
/// One element of the JSON ARRAY returned by POST /api/background-activities/configuration. The
/// customer-managed workflow agent unmarshals the body into <c>[]BackgroundActivityConfiguration</c> and
/// starts its spooler once it parses (confirmed against the live v2.2.0 agent). Fields beyond these are
/// org feature flags the agent tolerates being absent.
/// </summary>
public sealed class BackgroundActivityConfiguration
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = "deployment";
    [JsonPropertyName("interval")] public int Interval { get; set; } = 5;
    [JsonPropertyName("canChangeInterval")] public bool CanChangeInterval { get; set; } = true;
    [JsonPropertyName("concurrency")] public int Concurrency { get; set; } = 1;
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("agentPoolRegistrationEnabled")] public bool AgentPoolRegistrationEnabled { get; set; }
    [JsonPropertyName("agentPools")] public List<AgentPoolRef> AgentPools { get; set; } = new();
}

/// <summary>A pool the worker is registered to (subset the agent reads).</summary>
public sealed class AgentPoolRef
{
    [JsonPropertyName("id")] public string Id { get; set; } = default!;
    [JsonPropertyName("name")] public string Name { get; set; } = default!;
    [JsonPropertyName("description")] public string? Description { get; set; }
}

/// <summary>
/// The job definition the agent fetches from GET /api/workflow/jobs/{jobID} and hands to the
/// <c>workflow-runner</c>. Field layout mirrors the runner's <c>apitype.JobDefinition</c> exactly
/// (recovered from the binary): the runner builds a log redactor that dereferences <c>Image</c>, so a
/// non-null image is required to avoid the nil-pointer panic.
/// </summary>
public sealed class JobDefinition
{
    [JsonPropertyName("os")] public string Os { get; set; } = "linux";
    [JsonPropertyName("architecture")] public string Architecture { get; set; } = "amd64";
    [JsonPropertyName("image")] public JobImage Image { get; set; } = new();
    [JsonPropertyName("shell")] public string Shell { get; set; } = "bash";
    // Wire type is a duration string (apitype.WorkflowTimeout), not a number; omit when unset.
    [JsonPropertyName("timeout")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Timeout { get; set; }
    [JsonPropertyName("env")] public Dictionary<string, JobSecretValue> Env { get; set; } = new();
    [JsonPropertyName("steps")] public List<StepDefinition> Steps { get; set; } = new();
}

/// <summary>The executor container image the runner pulls (apitype.DockerImage wire shape).</summary>
public sealed class JobImage
{
    [JsonPropertyName("reference")] public string Reference { get; set; } = "pulumi/pulumi-base:latest";
}

/// <summary>A (possibly secret) job env value (apitype.SecretValue: value/ciphertext/secret).</summary>
public sealed class JobSecretValue
{
    [JsonPropertyName("value")] public string Value { get; set; } = "";
    [JsonPropertyName("secret")] public bool Secret { get; set; }
}

/// <summary>One step in a job definition. <c>Run</c> is the shell command the runner executes.</summary>
public sealed class StepDefinition
{
    [JsonPropertyName("name")] public string Name { get; set; } = default!;
    [JsonPropertyName("timeout")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Timeout { get; set; }
    [JsonPropertyName("run")] public string Run { get; set; } = default!;
    [JsonPropertyName("workingDir")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WorkingDir { get; set; }
}
