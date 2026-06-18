import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Layers, Rocket, Star, GitBranch, ChevronRight } from "lucide-react";
import { api, timeAgo, type Stack, type Deployment } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { StatusDot } from "../components/ui";

function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="rounded-xl border border-line bg-panel">
      <div className="border-b border-line px-4 py-3 text-sm font-semibold">{title}</div>
      <div className="p-4">{children}</div>
    </section>
  );
}

function EmptyMini({ icon: Icon, text }: { icon: typeof Layers; text: string }) {
  return (
    <div className="grid place-items-center gap-2 py-8 text-center text-sm text-ink-dim">
      <Icon size={20} className="text-ink-faint" />
      {text}
    </div>
  );
}

function Onboard({ icon: Icon, title, body, to }: { icon: typeof Rocket; title: string; body: string; to: string }) {
  return (
    <Link to={to} className="flex items-start gap-3 rounded-xl border border-line bg-panel p-4 transition-colors hover:bg-hover">
      <span className="grid size-9 place-items-center rounded-lg bg-brand/15 text-brand"><Icon size={18} /></span>
      <div className="flex-1">
        <div className="flex items-center gap-1 text-sm font-semibold">{title}<ChevronRight size={14} className="text-ink-faint" /></div>
        <p className="mt-0.5 text-xs text-ink-dim">{body}</p>
      </div>
    </Link>
  );
}

export default function Dashboard() {
  const org = useOrg();
  const [stacks, setStacks] = useState<Stack[]>([]);
  const [deps, setDeps] = useState<Deployment[]>([]);
  useEffect(() => {
    api.userStacks().then((r) => setStacks(r.stacks ?? []));
    api.orgDeployments(org).then((r) => setDeps(r.deployments ?? []));
  }, [org]);

  return (
    <div className="px-6 py-6">
      <h1 className="mb-1 text-2xl font-semibold">Welcome back</h1>
      <p className="mb-6 text-sm text-ink-dim">Your infrastructure at a glance.</p>

      <div className="mb-6 grid gap-4 md:grid-cols-2">
        <Onboard icon={Rocket} title="Run your first update" body="Deploy code and resource changes with pulumi up." to="/stacks" />
        <Onboard icon={GitBranch} title="Connect to version control" body="Ship faster and more safely with Git-driven deploys." to="/management/version-control" />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card title="Latest stack updates">
          {stacks.length === 0 ? <EmptyMini icon={Layers} text="No stack updates yet" /> : (
            <ul className="space-y-1">
              {stacks.slice(0, 6).map((s, i) => (
                <li key={i}>
                  <Link to={`/stacks/${s.projectName}/${s.stackName}`} className="flex items-center justify-between rounded-md px-2 py-1.5 text-sm hover:bg-hover">
                    <span className="flex items-center gap-2 truncate"><StatusDot status={s.lastUpdate?.result} />{s.projectName}/{s.stackName}</span>
                    <span className="text-xs text-ink-faint">{timeAgo(s.lastUpdate?.endTime ?? s.lastUpdate?.time ?? (s as { lastUpdate?: number }).lastUpdate)}</span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Card>
        <Card title="Favorite stacks"><EmptyMini icon={Star} text="Favorite stacks for quick access" /></Card>
        <Card title="Latest deployments">
          {deps.length === 0 ? <EmptyMini icon={Rocket} text="No deployments yet" /> : (
            <ul className="space-y-1">
              {deps.slice(0, 6).map((d) => (
                <li key={d.id}>
                  <Link to={`/deployments/${d.projectName}/${d.stackName}/${d.version}`} className="flex items-center justify-between rounded-md px-2 py-1.5 text-sm hover:bg-hover">
                    <span className="flex items-center gap-2 truncate"><StatusDot status={d.status} />{d.stackName} #{d.version}</span>
                    <span className="text-xs text-ink-faint">{timeAgo(d.created)}</span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>
    </div>
  );
}
