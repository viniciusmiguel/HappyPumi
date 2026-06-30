import { useMemo, useState } from "react";
import { Layers, ExternalLink } from "lucide-react";
import { type Resource } from "../../lib/api";
import { Badge, Table, EmptyState } from "../../components/ui";
import { urnName, providerOf, normalizeType, resourceIcon, CLOUD_LINKS } from "./resourceFormat";
import { ResourceDetail } from "./ResourceDetail";

export function Resources(
  { org, project, stack, resources }:
  { org: string; project: string; stack: string; resources: Resource[] },
) {
  const [q, setQ] = useState("");
  const [view, setView] = useState<"list" | "graph">("list");
  const [typeFilter, setTypeFilter] = useState("");
  const [selected, setSelected] = useState<Resource | null>(null);
  const types = useMemo(() => [...new Set(resources.map((r) => r.type))].sort(), [resources]);
  const rows = useMemo(() => resources.filter((r) =>
    `${r.type}${urnName(r.urn)}`.toLowerCase().includes(q.toLowerCase()) && (!typeFilter || r.type === typeFilter)),
    [resources, q, typeFilter]);
  if (resources.length === 0) return <EmptyState icon={Layers} title="No resources" description="This stack has no resources in its latest checkpoint." />;

  return (
    <div>
      <div className="mb-3 flex flex-wrap items-center gap-3">
        <div className="inline-flex overflow-hidden rounded-md border border-line text-sm">
          <button onClick={() => setView("list")} className={`px-3 py-1.5 ${view === "list" ? "bg-brand text-white" : "text-ink-dim hover:bg-hover"}`}>List view</button>
          <button onClick={() => setView("graph")} className={`px-3 py-1.5 ${view === "graph" ? "bg-brand text-white" : "text-ink-dim hover:bg-hover"}`}>Graph view</button>
        </div>
        <select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}
          className="rounded-md border border-line bg-panel px-2.5 py-1.5 text-sm outline-none">
          <option value="">All resources</option>
          {types.map((t) => <option key={t} value={t}>{normalizeType(t)}</option>)}
        </select>
        <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search for resources"
          className="w-64 rounded-md border border-line bg-panel px-2.5 py-1.5 text-sm outline-none placeholder:text-ink-faint" />
        <span className="ml-auto text-sm text-ink-dim">Resources: <Badge tone="brand">{resources.length}</Badge></span>
      </div>

      {view === "list" ? (
        <Table
          rows={rows}
          onRowClick={(r) => setSelected(r)}
          columns={[
            { header: "Type", cell: (r) => {
              const Icon = resourceIcon(r.type);
              return <span className="flex items-center gap-2"><Icon size={15} className="text-ink-faint" /><span className="font-mono text-xs">{normalizeType(r.type)}</span></span>;
            } },
            { header: "Name", cell: (r) => <span className="font-medium">{urnName(r.urn)}</span> },
            { header: "Status", cell: () => <span className="text-ink-faint">—</span> },
            { header: "Provider link", cell: (r) => {
              const prov = providerOf(r.type);
              const link = CLOUD_LINKS[prov];
              return link
                ? <a onClick={(e) => e.stopPropagation()} className="inline-flex items-center gap-1 text-brand hover:underline" href={link} target="_blank" rel="noreferrer">{prov} <ExternalLink size={12} /></a>
                : <span className="text-ink-faint">—</span>;
            } },
          ]}
        />
      ) : (
        <ResourceGraph resources={rows} onSelect={setSelected} />
      )}

      {selected && (
        <ResourceDetail org={org} project={project} stack={stack} resource={selected} onClose={() => setSelected(null)} />
      )}
    </div>
  );
}

// A simple parent->child resource graph derived from each resource's parent URN.
function ResourceGraph({ resources, onSelect }: { resources: Resource[]; onSelect: (r: Resource) => void }) {
  const byUrn = new Map(resources.map((r) => [r.urn, r]));
  const childrenOf = (urn: string) => resources.filter((r) => r.parent === urn);
  const roots = resources.filter((r) => !r.parent || !byUrn.has(r.parent));
  const Node = ({ r, depth }: { r: Resource; depth: number }) => {
    const Icon = resourceIcon(r.type);
    return (
      <div>
        <button onClick={() => onSelect(r)} className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-hover" style={{ paddingLeft: depth * 20 + 8 }}>
          <Icon size={15} className="text-ink-faint" />
          <span className="font-medium">{urnName(r.urn)}</span>
          <span className="font-mono text-xs text-ink-faint">{normalizeType(r.type)}</span>
        </button>
        {childrenOf(r.urn).map((c) => <Node key={c.urn} r={c} depth={depth + 1} />)}
      </div>
    );
  };
  return <div className="rounded-lg border border-line bg-panel p-2">{roots.map((r) => <Node key={r.urn} r={r} depth={0} />)}</div>;
}
