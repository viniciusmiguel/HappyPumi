import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Rocket, ChevronDown, ChevronRight } from "lucide-react";
import { api, timeAgo, type Deployment, type DeploymentStep, type DeploymentLogLine } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Breadcrumb, StatusDot, Badge, Card, Avatar } from "../components/ui";

export default function DeploymentDetail() {
  const org = useOrg();
  const { project = "", stack = "", version = "" } = useParams();
  const [dep, setDep] = useState<Deployment | null>(null);
  const [logs, setLogs] = useState<DeploymentLogLine[]>([]);

  useEffect(() => {
    api.deployment(org, project, stack, version).then((d) => {
      setDep(d);
      if (d.id) api.deploymentLogs(org, project, stack, d.id).then((r) => setLogs(r.lines ?? []));
    });
  }, [org, project, stack, version]);

  const job = dep?.jobs?.[0];
  return (
    <div className="px-6 py-5 pb-10">
      <div className="mb-2 flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <span className="grid size-7 place-items-center rounded-md border border-line bg-panel text-ink-dim"><Rocket size={16} /></span>
          <h1 className="text-xl font-semibold">Deployment #{version}</h1>
          {dep && <Badge tone={dep.status.includes("succe") ? "success" : dep.status.includes("fail") ? "danger" : "warn"}>{dep.status}</Badge>}
        </div>
      </div>
      <Breadcrumb items={[
        { label: "Deployments", to: "/deployments" },
        { label: `${project}/${stack}` },
        { label: `#${version}` },
      ]} />

      <div className="mt-4 grid gap-4 lg:grid-cols-3">
        <Card title="Summary">
          <div className="space-y-2 text-sm">
            <div className="flex justify-between"><span className="text-ink-dim">Operation</span><Badge>{dep?.pulumiOperation}</Badge></div>
            <div className="flex justify-between"><span className="text-ink-dim">Status</span><span className="flex items-center gap-2"><StatusDot status={dep?.status} />{dep?.status}</span></div>
            <div className="flex justify-between"><span className="text-ink-dim">Requested by</span><span className="flex items-center gap-2"><Avatar name={dep?.requestedBy?.name} size={18} />{dep?.requestedBy?.name}</span></div>
            <div className="flex justify-between"><span className="text-ink-dim">Started</span><span>{timeAgo(dep?.created)}</span></div>
          </div>
        </Card>
        <div className="lg:col-span-2">
          <Card title="Steps">
            {job?.steps?.length ? <ol className="space-y-1.5">{job.steps.map((s, i) => <Step key={i} step={s} />)}</ol>
              : <p className="text-sm text-ink-dim">No steps recorded.</p>}
          </Card>
        </div>
      </div>

      <div className="mt-4">
        <Card title="Deployment logs">
          <pre className="max-h-96 overflow-auto rounded-md bg-bg p-3 font-mono text-xs leading-relaxed text-ink-dim">
            {logs.length === 0 ? "No logs." : logs.map((l, i) => (
              <div key={i}>
                <span className="text-ink-faint">{l.header ? `[${l.header}] ` : ""}</span>{l.line}
              </div>
            ))}
          </pre>
        </Card>
      </div>
    </div>
  );
}

function Step({ step }: { step: DeploymentStep }) {
  const [open, setOpen] = useState(false);
  return (
    <li className="rounded-md border border-line">
      <button onClick={() => setOpen((v) => !v)} className="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-hover">
        {open ? <ChevronDown size={14} className="text-ink-faint" /> : <ChevronRight size={14} className="text-ink-faint" />}
        <StatusDot status={step.status} />
        <span className="flex-1 text-left">{step.name}</span>
        <span className="text-xs text-ink-faint">{step.status}</span>
      </button>
      {open && (
        <div className="border-t border-line px-9 py-2 text-xs text-ink-dim">
          Started {timeAgo(step.started)} · finished {timeAgo(step.lastUpdated)}
        </div>
      )}
    </li>
  );
}
