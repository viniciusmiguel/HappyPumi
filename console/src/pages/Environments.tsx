import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { KeyRound, Plus, Folder, ChevronDown, Search } from "lucide-react";
import { api, timeAgo, type OrgEnvironment } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, EmptyState, PrimaryButton } from "../components/ui";

export default function Environments() {
  const org = useOrg();
  const [envs, setEnvs] = useState<OrgEnvironment[]>([]);
  const [q, setQ] = useState("");

  useEffect(() => { api.environments(org).then((r) => setEnvs(r.environments ?? [])); }, [org]);

  const filtered = envs.filter((e) => `${e.project}/${e.name}`.includes(q));
  const groups = [...new Set(filtered.map((e) => e.project ?? "default"))].sort()
    .map((p) => ({ project: p, items: filtered.filter((e) => (e.project ?? "default") === p) }));
  const projectCount = new Set(envs.map((e) => e.project)).size;

  return (
    <div>
      <PageHeader icon={KeyRound} title="Environments"
        actions={
          <div className="flex items-center gap-3 text-sm text-ink-dim">
            <span>Projects: <span className="rounded bg-panel px-1.5 py-0.5">{projectCount}</span></span>
            <span>Environments: <span className="rounded bg-panel px-1.5 py-0.5">{envs.length}</span></span>
          </div>
        } />
      <div className="flex items-center justify-between px-6 py-3">
        <PrimaryButton icon={Plus}>Create Environment</PrimaryButton>
        <div className="flex items-center gap-2 rounded-md border border-line bg-panel px-2.5 py-1.5 text-sm">
          <Search size={14} className="text-ink-faint" />
          <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search"
            className="w-56 bg-transparent outline-none placeholder:text-ink-faint" />
        </div>
      </div>

      {envs.length === 0 ? (
        <EmptyState icon={KeyRound} title="No environments"
          description="You don't have any environments yet. Use the button above to create an environment." />
      ) : (
        <div className="space-y-3 px-6 pb-8">
          {groups.map((g) => (
            <div key={g.project} className="overflow-hidden rounded-lg border border-line">
              <div className="flex items-center justify-between border-b border-line bg-panel px-4 py-2.5">
                <div className="flex items-center gap-2 text-sm font-semibold">
                  <ChevronDown size={14} className="text-ink-faint" />
                  <Folder size={15} className="text-ink-faint" /> {g.project}
                  <span className="rounded bg-bg px-1.5 text-xs text-ink-dim">{g.items.length}</span>
                </div>
              </div>
              {g.items.map((e) => (
                <Link key={e.id} to={`/environments/${e.project}/${e.name}`}
                  className="flex items-center justify-between border-b border-line/60 px-4 py-2.5 text-sm last:border-0 hover:bg-hover">
                  <span className="flex items-center gap-2 pl-6"><KeyRound size={14} className="text-ink-faint" />{e.name}</span>
                  <span className="text-xs text-ink-faint">Updated {timeAgo(e.modified)}</span>
                </Link>
              ))}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
