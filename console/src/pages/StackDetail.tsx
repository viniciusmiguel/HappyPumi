import { useEffect, useMemo, useState } from "react";
import { Link, useParams, useSearchParams, useNavigate } from "react-router-dom";
import { Layers, ChevronDown, ExternalLink, RefreshCw, Download, Trash2 } from "lucide-react";
import {
  api, timeAgo, type Stack, type UpdateInfo, type Resource, type Deployment,
} from "../lib/api";
import { useOrg } from "../lib/useOrg";
import {
  Breadcrumb, Tabs, Badge, StatusDot, Table, Card, KeyValue, SecondaryButton,
  Avatar, EmptyState, Dropdown, Modal,
} from "../components/ui";

const TABS = [
  { key: "overview", label: "Overview" },
  { key: "readme", label: "README" },
  { key: "updates", label: "Updates" },
  { key: "deployments", label: "Deployments" },
  { key: "resources", label: "Resources" },
  { key: "settings", label: "Settings" },
];

function urnName(urn: string): string {
  return urn.split("::").pop() || urn;
}

// The provider/package name is the first segment of the type token ("aws:s3/bucketV2:BucketV2" -> "aws").
function providerOf(type: string): string {
  return type.split(":")[0] ?? "";
}

// Normalize "pkg:module/sub:Type" -> "pkg:module:Type" for display, matching the console.
function normalizeType(type: string): string {
  const [pkg, mod, name] = type.split(":");
  if (!mod || !name) return type;
  return `${pkg}:${mod.split("/")[0]}:${name}`;
}

// Cloud providers whose console we deep-link to; others (random, tls, pulumi) get no link.
const CLOUD_LINKS: Record<string, string> = {
  aws: "https://console.aws.amazon.com/",
  gcp: "https://console.cloud.google.com/",
  azure: "https://portal.azure.com/",
  "azure-native": "https://portal.azure.com/",
  azuread: "https://portal.azure.com/",
  kubernetes: "https://kubernetes.io/docs/",
  cloudflare: "https://dash.cloudflare.com/",
  digitalocean: "https://cloud.digitalocean.com/",
};

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
              { label: "Refresh", icon: RefreshCw, onSelect: () => navigate(`/deployments/${project}/${stack}/${lu?.version ?? 1}`) },
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
        {active === "overview" && <Overview meta={meta} count={count ?? resources.length} updates={updates} />}
        {active === "readme" && <Readme project={project} />}
        {active === "updates" && <Updates updates={updates} />}
        {active === "deployments" && <Deployments deps={deps} project={project} stack={stack} />}
        {active === "resources" && <Resources resources={resources} />}
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

function Overview({ meta, count, updates }: { meta: Stack | null; count: number; updates: UpdateInfo[] }) {
  const lu = meta?.lastUpdate;
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

function Updates({ updates }: { updates: UpdateInfo[] }) {
  if (updates.length === 0) return <EmptyState icon={Layers} title="No updates" description="Run pulumi up to create an update." />;
  return (
    <div className="space-y-2">
      {updates.map((u) => (
        <div key={u.version} className="flex items-center gap-3 rounded-lg border border-line bg-panel px-4 py-3">
          <StatusDot status={u.result} />
          <div className="flex-1">
            <div className="text-sm font-medium">{u.message || `${u.kind} #${u.version}`}</div>
            <div className="text-xs text-ink-faint">version {u.version} · {u.result}</div>
          </div>
          <div className="flex items-center gap-2 text-xs text-ink-dim">
            <Avatar name={u.requestedBy?.name} size={18} />{u.requestedBy?.name}
          </div>
          <span className="w-28 text-right text-xs text-ink-faint">{timeAgo(u.endTime ?? u.time)}</span>
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

function Resources({ resources }: { resources: Resource[] }) {
  const [q, setQ] = useState("");
  const rows = useMemo(() => resources.filter((r) => `${r.type}${urnName(r.urn)}`.toLowerCase().includes(q.toLowerCase())), [resources, q]);
  if (resources.length === 0) return <EmptyState icon={Layers} title="No resources" description="This stack has no resources in its latest checkpoint." />;
  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search for resources"
          className="w-72 rounded-md border border-line bg-panel px-2.5 py-1.5 text-sm outline-none placeholder:text-ink-faint" />
        <span className="text-sm text-ink-dim">Resources: <Badge tone="brand">{resources.length}</Badge></span>
      </div>
      <Table
        rows={rows}
        columns={[
          { header: "Type", cell: (r) => <span className="font-mono text-xs">{normalizeType(r.type)}</span> },
          { header: "Name", cell: (r) => <span className="font-medium">{urnName(r.urn)}</span> },
          { header: "Status", cell: () => <span className="text-ink-faint">—</span> },
          { header: "Provider link", cell: (r) => {
            const prov = providerOf(r.type);
            const link = CLOUD_LINKS[prov];
            return link
              ? <a className="inline-flex items-center gap-1 text-brand hover:underline" href={link} target="_blank" rel="noreferrer">{prov} <ExternalLink size={12} /></a>
              : <span className="text-ink-faint">—</span>;
          } },
        ]}
      />
    </div>
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
