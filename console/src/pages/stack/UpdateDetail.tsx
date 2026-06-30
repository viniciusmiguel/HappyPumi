import { useEffect, useState } from "react";
import { ArrowLeft } from "lucide-react";
import {
  api, timeAgo, type EngineEvent, type UpdateInfo, type UpdateSummaryDetail, type UpdateTimeline,
} from "../../lib/api";
import { Card, Badge, StatusDot, KeyValue } from "../../components/ui";

// Drill-in for a single update (reached from the Updates tab via ?update=<version>). Shows the summary,
// the persisted engine-event stream as a log, and the stack's previews. Engine events are keyed by the
// lifecycle update id, which the timeline's focal update carries.
export function UpdateDetail(
  { org, project, stack, version, onBack }:
  { org: string; project: string; stack: string; version: number; onBack: () => void },
) {
  const [summary, setSummary] = useState<UpdateSummaryDetail>({});
  const [timeline, setTimeline] = useState<UpdateTimeline>({});
  const [events, setEvents] = useState<EngineEvent[]>([]);

  useEffect(() => {
    api.stackUpdateSummary(org, project, stack, version).then(setSummary);
    api.stackUpdateTimeline(org, project, stack, version).then((t) => {
      setTimeline(t);
      const id = t.update?.updateID;
      if (id) api.stackUpdateEvents(org, project, stack, id).then((r) => setEvents(r.events ?? []));
    });
  }, [org, project, stack, version]);

  const duration = durationLabel(summary.startTime, summary.endTime);
  return (
    <div className="space-y-4">
      <button onClick={onBack} className="inline-flex items-center gap-1.5 text-sm text-ink-dim hover:text-ink">
        <ArrowLeft size={14} /> Back to updates
      </button>

      <Card title={`Update #${version}`}>
        <div className="space-y-0.5">
          <KeyValue label="Result"><span className="flex items-center gap-2"><StatusDot status={summary.result} />{summary.result ?? "—"}</span></KeyValue>
          <KeyValue label="Resources"><Badge tone="brand">{summary.resourceCount ?? 0}</Badge></KeyValue>
          <KeyValue label="Started"><span className="text-ink-dim">{timeAgo(summary.startTime)}</span></KeyValue>
          <KeyValue label="Duration"><span className="text-ink-dim">{duration}</span></KeyValue>
        </div>
      </Card>

      <Card title="Engine events">
        {events.length === 0 ? <p className="text-sm text-ink-dim">No engine events were recorded for this update.</p> : (
          <div className="max-h-96 overflow-auto rounded-md border border-line bg-bg font-mono text-xs">
            {events.map((e, i) => (
              <div key={i} className="flex gap-3 border-b border-line px-3 py-1 last:border-0">
                <span className="w-16 shrink-0 text-ink-faint">{eventTime(e.timestamp)}</span>
                <span className="break-all">{eventLine(e)}</span>
              </div>
            ))}
          </div>
        )}
      </Card>

      <Card title="Previews">
        {(timeline.previews?.length ?? 0) === 0 ? <p className="text-sm text-ink-dim">No previews for this stack.</p> : (
          <div className="space-y-1.5">
            {timeline.previews!.map((p) => <PreviewRow key={p.updateID ?? p.version} preview={p} />)}
          </div>
        )}
      </Card>
    </div>
  );
}

function PreviewRow({ preview }: { preview: UpdateInfo }) {
  return (
    <div className="flex items-center gap-3 rounded-md border border-line px-3 py-1.5 text-sm">
      <StatusDot status={preview.result} />
      <span className="flex-1">preview #{preview.version} {preview.result}</span>
      <span className="text-xs text-ink-faint">{timeAgo(preview.startTime ?? preview.endTime)}</span>
    </div>
  );
}

function eventTime(ts?: number): string {
  if (!ts) return "";
  return new Date(ts * 1000).toLocaleTimeString();
}

function eventLine(e: EngineEvent): string {
  if (e.diagnosticEvent?.message) return e.diagnosticEvent.message.trimEnd();
  if (e.stdoutEvent?.message) return e.stdoutEvent.message.trimEnd();
  const step = e.resourcePreEvent?.metadata ?? e.resOutputsEvent?.metadata;
  if (step?.urn) return `${step.op ?? "step"} ${step.urn}`;
  if (e.summaryEvent?.resourceChanges) {
    const parts = Object.entries(e.summaryEvent.resourceChanges).map(([k, v]) => `${v} ${k}`);
    return `summary: ${parts.join(", ")}`;
  }
  return e.type ?? "event";
}

function durationLabel(start?: number, end?: number): string {
  if (!start || !end || end < start) return "—";
  const secs = end - start;
  if (secs < 60) return `${secs}s`;
  return `${Math.floor(secs / 60)}m ${secs % 60}s`;
}
