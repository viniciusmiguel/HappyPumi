#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Endpoints.Stacks;

/// <summary>
/// Maps stored <see cref="AppEngineEvent"/> records to the wire <see cref="EngineEvent"/> the read-side
/// returns, and derives the resource count an update/preview summary reports from those events.
/// </summary>
internal static class EngineEventMapper
{
    // Discriminator: the JS contract guarantees event[event.type] is the populated payload (EngineEvent doc).
    private static readonly (Func<AppEngineEvent, object?> Payload, string Name)[] Kinds =
    {
        (e => e.CancelEvent, "cancelEvent"),
        (e => e.DiagnosticEvent, "diagnosticEvent"),
        (e => e.ErrorEvent, "errorEvent"),
        (e => e.PolicyAnalyzeStackSummaryEvent, "policyAnalyzeStackSummaryEvent"),
        (e => e.PolicyAnalyzeSummaryEvent, "policyAnalyzeSummaryEvent"),
        (e => e.PolicyEvent, "policyEvent"),
        (e => e.PolicyLoadEvent, "policyLoadEvent"),
        (e => e.PolicyRemediateSummaryEvent, "policyRemediateSummaryEvent"),
        (e => e.PolicyRemediationEvent, "policyRemediationEvent"),
        (e => e.PreludeEvent, "preludeEvent"),
        (e => e.ProgressEvent, "progressEvent"),
        (e => e.ResOpFailedEvent, "resOpFailedEvent"),
        (e => e.ResOutputsEvent, "resOutputsEvent"),
        (e => e.ResourcePreEvent, "resourcePreEvent"),
        (e => e.StartDebuggingEvent, "startDebuggingEvent"),
        (e => e.StdoutEvent, "stdoutEvent"),
        (e => e.SummaryEvent, "summaryEvent"),
    };

    public static EngineEvent ToWire(AppEngineEvent e) => new()
    {
        Timestamp = e.Timestamp,
        Type = TypeOf(e),
        CancelEvent = e.CancelEvent,
        DiagnosticEvent = e.DiagnosticEvent,
        ErrorEvent = e.ErrorEvent,
        PolicyAnalyzeStackSummaryEvent = e.PolicyAnalyzeStackSummaryEvent,
        PolicyAnalyzeSummaryEvent = e.PolicyAnalyzeSummaryEvent,
        PolicyEvent = e.PolicyEvent,
        PolicyLoadEvent = e.PolicyLoadEvent,
        PolicyRemediateSummaryEvent = e.PolicyRemediateSummaryEvent,
        PolicyRemediationEvent = e.PolicyRemediationEvent,
        PreludeEvent = e.PreludeEvent,
        ProgressEvent = e.ProgressEvent,
        ResOpFailedEvent = e.ResOpFailedEvent,
        ResOutputsEvent = e.ResOutputsEvent,
        ResourcePreEvent = e.ResourcePreEvent,
        StartDebuggingEvent = e.StartDebuggingEvent,
        StdoutEvent = e.StdoutEvent,
        SummaryEvent = e.SummaryEvent,
    };

    private static string TypeOf(AppEngineEvent e)
        => Kinds.FirstOrDefault(k => k.Payload(e) is not null).Name ?? string.Empty;

    /// <summary>
    /// Resource count for a summary: prefers the final SummaryEvent's change totals, else the number of
    /// distinct resources touched by ResourcePreEvents. Zero when no events were recorded.
    /// </summary>
    public static long ResourceCount(IReadOnlyList<AppEngineEvent> events)
    {
        var changes = events.LastOrDefault(e => e.SummaryEvent is not null)?.SummaryEvent?.ResourceChanges;
        if (changes is { Count: > 0 })
            return changes.Values.Sum();

        return events
            .Where(e => e.ResourcePreEvent?.Metadata?.Urn is { Length: > 0 })
            .Select(e => e.ResourcePreEvent!.Metadata.Urn)
            .Distinct()
            .Count();
    }
}
