#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Maps stored audit rows/config (ADR-0010) to the audit-log wire contracts and builds the CSV export.
/// Shared by the v2 list, both export handlers, and the export-config endpoints so the mapping lives in one
/// place.
/// </summary>
public static class AuditLogMapper
{
    /// <summary>Maps a stored audit row to the wire event, converting the timestamp to unix seconds.</summary>
    public static AuditLogEvent ToEvent(AuditLogRow row) => new()
    {
        Event = row.Event,
        Description = row.Description,
        ActorName = row.ActorName,
        SourceIp = row.SourceIp,
        Timestamp = new DateTimeOffset(row.Timestamp, TimeSpan.Zero).ToUnixTimeSeconds(),
        User = new UserInfo { GithubLogin = row.ActorName, Name = row.ActorName },
    };

    /// <summary>Maps rows to events (newest-first preserved) filtered by event substring and time window.</summary>
    public static List<AuditLogEvent> Filter(IEnumerable<AuditLogRow> rows, string? eventFilter, long? startTime, long? endTime)
        => rows.Select(ToEvent)
            .Where(e => Matches(e, eventFilter, startTime, endTime))
            .ToList();

    private static bool Matches(AuditLogEvent e, string? eventFilter, long? startTime, long? endTime)
    {
        if (!string.IsNullOrEmpty(eventFilter) && !e.Event.Contains(eventFilter, StringComparison.OrdinalIgnoreCase))
            return false;
        if (startTime is { } lo && e.Timestamp < lo)
            return false;
        return endTime is not { } hi || e.Timestamp <= hi;
    }

    /// <summary>Maps the stored export config to the wire settings shape (empty strings for unset fields).</summary>
    public static OrganizationAuditLogExportSettings ToSettings(StoredAuditExportConfig c) => new()
    {
        Enabled = c.Enabled,
        LastResult = new AuditLogExportResult { Message = c.LastResultMessage ?? "", Timestamp = c.LastResultTimestamp },
        S3Config = new AuditLogsExportS3Config
        {
            IamRoleArn = c.IamRoleArn ?? "",
            S3BucketName = c.S3BucketName ?? "",
            S3PathPrefix = c.S3PathPrefix,
        },
    };

    /// <summary>Renders events as CSV: a header row plus one row per event, RFC-4180 escaped.</summary>
    public static string ToCsv(IEnumerable<AuditLogEvent> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("timestamp,event,actor,sourceIP,description");
        foreach (var e in events)
            sb.AppendLine(Row(e));
        return sb.ToString();
    }

    private static string Row(AuditLogEvent e) => string.Join(',', new[]
    {
        e.Timestamp.ToString(CultureInfo.InvariantCulture),
        Escape(e.Event),
        Escape(e.ActorName ?? ""),
        Escape(e.SourceIp ?? ""),
        Escape(e.Description ?? ""),
    });

    private static string Escape(string field)
    {
        if (!field.Contains(',') && !field.Contains('"') && !field.Contains('\n') && !field.Contains('\r'))
            return field;
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}
