import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Activity as ActivityIcon } from "lucide-react";
import { api, timeAgo, type UpdateInfo } from "../../lib/api";
import { StatusDot, Avatar, Badge, EmptyState } from "../../components/ui";

// The Activity tab: a flat, newest-first feed of stack updates (paginated). Rows drill into the
// update detail on the Updates tab via ?tab=updates&update=<version>.
export function Activity({ org, project, stack }: { org: string; project: string; stack: string }) {
  const [, setParams] = useSearchParams();
  const [items, setItems] = useState<UpdateInfo[]>([]);
  const [total, setTotal] = useState(0);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    api.stackActivity(org, project, stack).then((r) => {
      setItems(r.activity.map((a) => a.update).filter((u): u is UpdateInfo => !!u));
      setTotal(r.total);
      setLoaded(true);
    });
  }, [org, project, stack]);

  const open = (version: number) => setParams({ tab: "updates", update: String(version) }, { replace: true });

  if (loaded && items.length === 0) {
    return <EmptyState icon={ActivityIcon} title="No activity" description="Updates to this stack appear here." />;
  }

  return (
    <div>
      <div className="mb-3 text-sm text-ink-dim">Activity: <Badge tone="brand">{total}</Badge></div>
      <div className="space-y-2">
        {items.map((u) => (
          <button key={u.version} onClick={() => open(u.version)}
            className="flex w-full items-center gap-3 rounded-lg border border-line bg-panel px-4 py-3 text-left hover:bg-hover">
            <StatusDot status={u.result} />
            <div className="flex-1">
              <div className="text-sm font-medium">{u.kind ?? "update"} #{u.version} {u.result}</div>
              <div className="flex items-center gap-1.5 text-xs text-ink-faint">
                <Avatar name={u.requestedBy?.name} size={14} />
                {u.requestedBy?.githubLogin ?? u.requestedBy?.name ?? "unknown"} · {timeAgo(u.endTime ?? u.time)}
              </div>
            </div>
          </button>
        ))}
      </div>
    </div>
  );
}
