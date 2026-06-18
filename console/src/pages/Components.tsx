import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Boxes, Search } from "lucide-react";
import { api, timeAgo, type RegistryPackage } from "../lib/api";
import { PageHeader, Table, EmptyState, Avatar } from "../components/ui";

export default function Components() {
  const [pkgs, setPkgs] = useState<RegistryPackage[]>([]);
  const [q, setQ] = useState("");

  useEffect(() => { api.packages().then((r) => setPkgs(r.packages ?? [])); }, []);
  const rows = pkgs.filter((p) => p.name.includes(q));

  return (
    <div>
      <PageHeader icon={Boxes} title="Private Components" />
      <p className="px-6 pt-3 text-sm text-ink-dim">
        Components are higher-level building blocks with best practices and sensible defaults built in so you can
        spend less time on configuration and more time building applications.
      </p>
      <div className="px-6 py-3">
        <div className="flex w-96 items-center gap-2 rounded-md border border-line bg-panel px-2.5 py-1.5 text-sm">
          <Search size={14} className="text-ink-faint" />
          <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search components"
            className="flex-1 bg-transparent outline-none placeholder:text-ink-faint" />
        </div>
      </div>
      <Table
        rows={rows}
        columns={[
          { header: "Component", cell: (p) => (
            <Link to={`/platform/components/${p.source}/${p.publisher}/${p.name}`} className="font-medium hover:underline">{p.name}</Link>
          ) },
          { header: "Latest version", cell: (p) => <span className="text-ink-dim">{p.version}</span> },
          { header: "Publisher", cell: (p) => <span className="flex items-center gap-2"><Avatar name={p.publisher} size={18} />{p.publisher}</span> },
          { header: "Stacks on latest", cell: () => <span className="text-ink-faint">—</span> },
          { header: "Total stacks", cell: () => <span className="text-ink-faint">—</span> },
          { header: "Last published", cell: (p) => <span className="text-ink-dim">{timeAgo(p.createdAt)}</span> },
        ]}
        empty={<EmptyState icon={Boxes} title="No components"
          description="Publish a component to your organization's private registry to share it with your teams." />}
      />
    </div>
  );
}
