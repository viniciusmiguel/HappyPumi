import { useSearchParams } from "react-router-dom";
import { Layers } from "lucide-react";
import { timeAgo, type UpdateInfo } from "../../lib/api";
import { Badge, StatusDot, Avatar, EmptyState } from "../../components/ui";
import { UpdateDetail } from "./UpdateDetail";

// The Updates tab: a day-grouped activity list whose rows drill into UpdateDetail via a ?update=<version>
// search param (same convention as the ?tab= router), so deep links to one update survive a reload.
export function Updates(
  { org, project, stack, updates }:
  { org: string; project: string; stack: string; updates: UpdateInfo[] },
) {
  const [params, setParams] = useSearchParams();
  const selected = params.get("update");

  const open = (version: number) => {
    const next = new URLSearchParams(params);
    next.set("update", String(version));
    setParams(next, { replace: false });
  };
  const back = () => {
    const next = new URLSearchParams(params);
    next.delete("update");
    setParams(next, { replace: false });
  };

  if (selected != null) {
    return <UpdateDetail org={org} project={project} stack={stack} version={Number(selected)} onBack={back} />;
  }
  if (updates.length === 0) return <EmptyState icon={Layers} title="No updates" description="Run pulumi up to create an update." />;

  return (
    <div className="space-y-5">
      {groupByDay(updates).map((g) => (
        <div key={g.day}>
          <h3 className="mb-2 text-sm font-semibold text-ink-dim">{g.day}</h3>
          <div className="space-y-2">
            {g.items.map((u) => (
              <button key={u.version} onClick={() => open(u.version)}
                className="block w-full rounded-lg border border-line bg-panel text-left hover:border-brand/50">
                <div className="flex items-center gap-3 px-4 py-3">
                  <StatusDot status={u.result} />
                  <div className="flex-1">
                    <div className="text-sm font-medium">{u.kind ?? "update"} #{u.version} {u.result}</div>
                    <div className="flex items-center gap-1.5 text-xs text-ink-faint">
                      <Avatar name={u.requestedBy?.name} size={14} />
                      {u.requestedBy?.githubLogin ?? u.requestedBy?.name ?? "unknown"} updated {timeAgo(u.endTime ?? u.time)}
                    </div>
                  </div>
                  <Badge>{changeCount(u)}</Badge>
                </div>
              </button>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

function changeCount(u: UpdateInfo): number {
  if (u.resourceCount != null) return u.resourceCount;
  const c = u.resourceChanges ?? u.info?.resourceChanges;
  return c ? Object.values(c).reduce((a, b) => a + (b || 0), 0) : 0;
}

function updateDay(u: UpdateInfo): string {
  const ms = (u.endTime ?? u.time ?? 0) * 1000;
  if (!ms) return "Activity";
  return "Activity on " + new Date(ms).toLocaleDateString(undefined, { month: "long", day: "numeric", year: "numeric" });
}

// Group updates into the real console's "Activity on <date>" sections, preserving input order.
function groupByDay(updates: UpdateInfo[]): { day: string; items: UpdateInfo[] }[] {
  const groups: { day: string; items: UpdateInfo[] }[] = [];
  for (const u of updates) {
    const day = updateDay(u);
    const g = groups.find((x) => x.day === day) ?? (groups.push({ day, items: [] }), groups[groups.length - 1]);
    g.items.push(u);
  }
  return groups;
}
