import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Layers, Rocket, Star, GitBranch, ChevronRight } from "lucide-react";
import { api, type StackSummary } from "../lib/api";

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
  const [stacks, setStacks] = useState<StackSummary[]>([]);
  useEffect(() => { api.userStacks().then((r) => setStacks(r.stacks ?? [])); }, []);

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
                  <Link to="/stacks" className="flex items-center justify-between rounded-md px-2 py-1.5 text-sm hover:bg-hover">
                    <span className="truncate">{s.projectName}/{s.stackName}</span>
                    <span className="text-xs text-ink-faint">{s.orgName}</span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Card>
        <Card title="Favorite stacks"><EmptyMini icon={Star} text="Favorite stacks for quick access" /></Card>
        <Card title="Latest deployments"><EmptyMini icon={Rocket} text="No deployments yet" /></Card>
      </div>
    </div>
  );
}
