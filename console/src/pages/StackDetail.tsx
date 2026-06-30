import { useEffect, useState } from "react";
import { Link, useParams, useSearchParams, useNavigate } from "react-router-dom";
import { Layers, ChevronDown, Download, Trash2 } from "lucide-react";
import {
  api, timeAgo, type Stack, type UpdateInfo, type Resource, type Deployment,
} from "../lib/api";
import { useOrg } from "../lib/useOrg";
import {
  Breadcrumb, Tabs, Badge, StatusDot, Table, Card, SecondaryButton,
  Avatar, EmptyState, Dropdown, Modal,
} from "../components/ui";
import { Overview } from "./stack/Overview";
import { Resources } from "./stack/Resources";

const TABS = [
  { key: "overview", label: "Overview" },
  { key: "readme", label: "README" },
  { key: "updates", label: "Updates" },
  { key: "deployments", label: "Deployments" },
  { key: "resources", label: "Resources" },
  { key: "settings", label: "Settings" },
];

export default function StackDetail() {
  const org = useOrg();
  const navigate = useNavigate();
  const { project = "", stack = "" } = useParams();
  const [params, setParams] = useSearchParams();
  const active = params.get("tab") || "overview";
  const [confirmDelete, setConfirmDelete] = useState(false);

  const [meta, setMeta] = useState<Stack | null>(null);
  const [updates, setUpdates] = useState<UpdateInfo[]>([]);
  const [resources, setResources] = useState<Resource[]>([]);
  const [count, setCount] = useState<number | undefined>();
  const [deps, setDeps] = useState<Deployment[]>([]);

  useEffect(() => {
    api.stackMetadata(org, project, stack).then(setMeta);
    api.stackUpdates(org, project, stack).then((r) => setUpdates(r.updates ?? []));
    api.stackResources(org, project, stack).then((r) => setResources((r.resources ?? []).map((x) => x.resource)));
    api.stackResourceCount(org, project, stack).then((r) => setCount(r.resourceCount));
    api.orgDeployments(org).then((r) => setDeps((r.deployments ?? []).filter((d) => d.stackName === stack && d.projectName === project)));
  }, [org, project, stack]);

  const lu = meta?.lastUpdate;
  return (
    <div className="pb-10">
      {/* header */}
      <div className="px-6 pt-5">
        <div className="mb-2 flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <span className="grid size-7 place-items-center rounded-md border border-line bg-panel text-ink-dim"><Layers size={16} /></span>
            <h1 className="text-xl font-semibold">{stack}</h1>
          </div>
          <Dropdown
            trigger={<SecondaryButton icon={ChevronDown}>Actions</SecondaryButton>}
            items={[
              { label: "Export checkpoint", icon: Download, onSelect: () => window.open(`/api/stacks/${org}/${project}/${stack}/export`, "_blank") },
              { label: "Delete stack", icon: Trash2, danger: true, onSelect: () => setConfirmDelete(true) },
            ]} />
        </div>
        <Breadcrumb items={[{ label: "Stacks", to: "/stacks" }, { label: project }, { label: stack }]} />
        <div className="mt-3 flex items-center gap-2 text-sm">
          <span className="font-semibold">Status:</span>
          <StatusDot status={lu?.result} />
          <span className="text-ink-dim">
            Update #{lu?.version ?? "—"} {lu?.result ?? "—"} {timeAgo(lu?.endTime ?? lu?.time)}
          </span>
        </div>
      </div>

      <div className="mt-4">
        <Tabs tabs={TABS.map((t) => t.key === "resources" ? { ...t, badge: count ?? resources.length } : t)}
          active={active} onChange={(k) => setParams({ tab: k }, { replace: true })} />
      </div>

      <div className="px-6 py-5">
        {active === "overview" && <Overview org={org} project={project} stack={stack} meta={meta} count={count ?? resources.length} updates={updates} />}
        {active === "readme" && <Readme project={project} />}
        {active === "updates" && <Updates updates={updates} />}
        {active === "deployments" && <Deployments deps={deps} project={project} stack={stack} />}
        {active === "resources" && <Resources org={org} project={project} stack={stack} resources={resources} />}
        {active === "settings" && <Settings meta={meta} onDelete={() => setConfirmDelete(true)} />}
      </div>

      {confirmDelete && (
        <Modal title="Delete stack" onClose={() => setConfirmDelete(false)}
          footer={<>
            <SecondaryButton onClick={() => setConfirmDelete(false)}>Cancel</SecondaryButton>
            <button onClick={() => navigate("/stacks")}
              className="rounded-md bg-red-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-red-500">Delete stack</button>
          </>}>
          <p className="text-sm text-ink-dim">
            This permanently deletes <b className="text-ink">{project}/{stack}</b> and all its update history.
            This cannot be undone.
          </p>
        </Modal>
      )}
    </div>
  );
}

function Readme({ project }: { project: string }) {
  return (
    <Card>
      <h2 className="text-lg font-semibold">{project}</h2>
      <p className="mt-2 text-sm text-ink-dim">
        This stack has no README. Add a <code className="rounded bg-bg px-1">Pulumi.README.md</code> to your project to document it here.
      </p>
    </Card>
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

function Updates({ updates }: { updates: UpdateInfo[] }) {
  if (updates.length === 0) return <EmptyState icon={Layers} title="No updates" description="Run pulumi up to create an update." />;
  // Group by calendar day, matching the real console's "Activity on <date>" sections.
  const groups: { day: string; items: UpdateInfo[] }[] = [];
  for (const u of updates) {
    const day = updateDay(u);
    const g = groups.find((x) => x.day === day) ?? (groups.push({ day, items: [] }), groups[groups.length - 1]);
    g.items.push(u);
  }
  return (
    <div className="space-y-5">
      {groups.map((g) => (
        <div key={g.day}>
          <h3 className="mb-2 text-sm font-semibold text-ink-dim">{g.day}</h3>
          <div className="space-y-2">
            {g.items.map((u) => (
              <div key={u.version} className="rounded-lg border border-line bg-panel">
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
              </div>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

function Deployments({ deps, project, stack }: { deps: Deployment[]; project: string; stack: string }) {
  if (deps.length === 0) return <EmptyState icon={Layers} title="No deployments" description="Deployments triggered for this stack appear here." />;
  return (
    <Table
      rows={deps}
      columns={[
        { header: "Deployment", cell: (d) => (
          <Link to={`/deployments/${project}/${stack}/${d.version}`} className="font-medium hover:underline">#{d.version}</Link>
        ) },
        { header: "Operation", cell: (d) => <Badge>{d.pulumiOperation}</Badge> },
        { header: "Status", cell: (d) => <span className="flex items-center gap-2"><StatusDot status={d.status} />{d.status}</span> },
        { header: "Started", cell: (d) => <span className="text-ink-dim">{timeAgo(d.created)}</span> },
      ]}
    />
  );
}

function Settings({ meta, onDelete }: { meta: Stack | null; onDelete: () => void }) {
  const tags = Object.entries(meta?.tags ?? {});
  return (
    <div className="max-w-2xl space-y-4">
      <Card title="Stack tags">
        {tags.length === 0 ? <p className="text-sm text-ink-dim">No tags.</p> : (
          <div className="space-y-1.5">
            {tags.map(([k, v]) => (
              <div key={k} className="flex items-center justify-between rounded-md border border-line px-3 py-1.5 text-sm">
                <span className="font-mono text-xs text-ink-dim">{k}</span><span>{v}</span>
              </div>
            ))}
          </div>
        )}
      </Card>
      <Card title="Danger zone">
        <button onClick={onDelete} className="rounded-md border border-red-500/40 px-3 py-1.5 text-sm text-red-400 hover:bg-red-500/10">Delete stack</button>
      </Card>
    </div>
  );
}
