#nullable enable

using System;

namespace HappyPumi.Api.Data.Entities;

/// <summary>
/// A persisted org template source (templates PR1, ADR-0005). Key: (Org, Id). All fields are scalar columns
/// — there is no nested payload — so the list/read endpoints can query them directly.
/// </summary>
public sealed class TemplateSourceRow
{
    public string Id { get; set; } = default!;
    public string Org { get; set; } = default!;
    public string Name { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string? DestinationUrl { get; set; }
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
