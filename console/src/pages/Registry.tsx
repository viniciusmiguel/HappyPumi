import { useEffect, useMemo, useState } from "react";
import { Package, Search } from "lucide-react";
import { api, type RegistryPackage } from "../lib/api";
import { PageHeader, Table, EmptyState } from "../components/ui";

export default function Registry() {
  const [packages, setPackages] = useState<RegistryPackage[]>([]);
  const [q, setQ] = useState("");
  useEffect(() => { api.packages().then((r) => setPackages(r.packages ?? [])); }, []);

  const rows = useMemo(
    () => packages.filter((p) => p.name?.toLowerCase().includes(q.toLowerCase())),
    [packages, q],
  );

  return (
    <div>
      <PageHeader icon={Package} title="Registry" />
      <div className="px-6 py-4">
        <div className="flex items-center gap-2 rounded-md border border-line bg-panel px-3 py-2">
          <Search size={15} className="text-ink-faint" />
          <input
            value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search packages"
            className="w-full bg-transparent text-sm outline-none placeholder:text-ink-faint"
          />
        </div>
      </div>
      <Table
        rows={rows}
        columns={[
          { header: "Package", cell: (p) => <span className="font-medium">{p.name}</span> },
          { header: "Latest version", cell: (p) => <span className="text-ink-dim">{p.version ?? "—"}</span> },
          { header: "Publisher", cell: (p) => <span className="text-ink-dim">{p.publisher ?? p.source ?? "—"}</span> },
        ]}
        empty={<EmptyState icon={Package} title="No packages"
          description="The private registry lets developers discover components and templates, browse their APIs, and read usage docs." />}
      />
    </div>
  );
}
