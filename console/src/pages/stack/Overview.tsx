import { useEffect, useMemo, useState } from "react";
import { api, timeAgo, type Stack, type UpdateInfo, type StackOverview } from "../../lib/api";
import { Card, KeyValue, StatusDot, Avatar, Badge } from "../../components/ui";
import { normalizeType } from "./resourceFormat";

export function Overview(
  { org, project, stack, meta, count, updates }:
  { org: string; project: string; stack: string; meta: Stack | null; count: number; updates: UpdateInfo[] },
) {
  const lu = meta?.lastUpdate;
  const [overview, setOverview] = useState<StackOverview | null>(null);

  useEffect(() => {
    api.stackOverview(org, project, stack).then(setOverview);
  }, [org, project, stack]);

  // Resource-count-by-type breakdown from the overview aggregation (top types shown).
  const byType = useMemo(() => {
    const counts = new Map<string, number>();
    for (const { resource } of overview?.resources?.resources ?? []) {
      const t = normalizeType(resource.type);
      counts.set(t, (counts.get(t) ?? 0) + 1);
    }
    return [...counts.entries()].sort((a, b) => b[1] - a[1]).slice(0, 8);
  }, [overview]);

  const tags = Object.entries(overview?.tags ?? meta?.tags ?? {});

  return (
    <div className="grid gap-4 lg:grid-cols-3">
      <Card title="Stack" className="lg:col-span-2">
        <KeyValue label="Project">{meta?.projectName}</KeyValue>
        <KeyValue label="Last update">
          <span className="flex items-center gap-2"><StatusDot status={lu?.result} />Update #{lu?.version} · {timeAgo(lu?.endTime ?? lu?.time)}</span>
        </KeyValue>
        <KeyValue label="Updated by">
          <span className="flex items-center gap-2"><Avatar name={lu?.requestedBy?.name} size={20} />{lu?.requestedBy?.name ?? "—"}</span>
        </KeyValue>
        <KeyValue label="Resources">{count}</KeyValue>
        {tags.length > 0 && (
          <KeyValue label="Tags">
            <span className="flex flex-wrap gap-1.5">
              {tags.map(([k, v]) => <Badge key={k}>{k}: {v}</Badge>)}
            </span>
          </KeyValue>
        )}
      </Card>

      <Card title="Recent activity">
        {updates.length === 0 ? <p className="text-sm text-ink-dim">No updates yet.</p> : (
          <ul className="space-y-2 text-sm">
            {updates.slice(0, 5).map((u) => (
              <li key={u.version} className="flex items-center gap-2">
                <StatusDot status={u.result} />
                <span className="flex-1 truncate">{u.kind} #{u.version}</span>
                <span className="text-xs text-ink-faint">{timeAgo(u.endTime ?? u.time)}</span>
              </li>
            ))}
          </ul>
        )}
      </Card>

      {byType.length > 0 && (
        <Card title="Resource types" className="lg:col-span-3">
          <div className="flex flex-wrap gap-2">
            {byType.map(([t, n]) => (
              <span key={t} className="inline-flex items-center gap-1.5 rounded-md border border-line bg-panel px-2.5 py-1 text-xs">
                <span className="font-mono">{t}</span><Badge tone="brand">{n}</Badge>
              </span>
            ))}
          </div>
        </Card>
      )}
    </div>
  );
}
