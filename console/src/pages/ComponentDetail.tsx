import { useEffect, useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { Boxes } from "lucide-react";
import { api, timeAgo, type RegistryPackage, type PackageNav } from "../lib/api";
import { Breadcrumb, Tabs, Table, Card, KeyValue, Avatar, Markdown, Badge, EmptyState } from "../components/ui";

const TABS = [
  { key: "overview", label: "Overview" },
  { key: "versions", label: "Versions" },
  { key: "usedby", label: "Used by" },
  { key: "apidocs", label: "API Docs" },
];

export default function ComponentDetail() {
  const { source = "", publisher = "", name = "" } = useParams();
  const [params, setParams] = useSearchParams();
  const active = params.get("tab") || "overview";

  const [meta, setMeta] = useState<RegistryPackage | null>(null);
  const [readme, setReadme] = useState("");
  const [versions, setVersions] = useState<RegistryPackage[]>([]);
  const [nav, setNav] = useState<PackageNav | null>(null);

  useEffect(() => {
    api.packageVersion(source, publisher, name).then((m) => {
      setMeta(m);
      if (m.version) api.packageReadme(source, publisher, name, m.version).then(setReadme);
    });
    api.packageVersions(source, publisher, name).then((r) => setVersions(r.packages ?? []));
    api.packageNav(source, publisher, name).then(setNav);
  }, [source, publisher, name]);

  return (
    <div className="pb-10">
      <div className="px-6 pt-5">
        <h1 className="mb-2 text-xl font-semibold">{name}</h1>
        <Breadcrumb items={[{ label: "Private Components", to: "/platform/components" }, { label: name }]} />
      </div>
      <div className="mt-4">
        <Tabs tabs={TABS} active={active} onChange={(k) => setParams({ tab: k }, { replace: true })} />
      </div>
      <div className="px-6 py-5">
        {active === "overview" && <Overview meta={meta} readme={readme} />}
        {active === "versions" && <Versions versions={versions} />}
        {active === "usedby" && <EmptyState icon={Boxes} title="No stacks found"
          description="This package is not used by any stacks in your organization." />}
        {active === "apidocs" && <ApiDocs nav={nav} />}
      </div>
    </div>
  );
}

function Overview({ meta, readme }: { meta: RegistryPackage | null; readme: string }) {
  return (
    <div className="max-w-4xl">
      <Card className="mb-4">
        <KeyValue label="Name">{meta?.name}</KeyValue>
        <KeyValue label="Publisher"><span className="flex items-center gap-2"><Avatar name={meta?.publisher} size={20} />{meta?.publisher}</span></KeyValue>
        <KeyValue label="Version"><Badge>{meta?.version} <span className="ml-1 opacity-70">Latest</span></Badge></KeyValue>
      </Card>
      <div className="text-lg font-semibold">README</div>
      <div className="mt-2 rounded-lg border border-line p-4">
        {readme ? <Markdown source={readme} /> : <p className="text-sm text-ink-dim">No README.</p>}
      </div>
    </div>
  );
}

function Versions({ versions }: { versions: RegistryPackage[] }) {
  return (
    <Table
      rows={versions}
      columns={[
        { header: "Version", cell: (v) => <span className="font-medium text-brand">{v.version}</span> },
        { header: "Status", cell: (v) => v.isLatest ? <Badge tone="success">Latest</Badge> : <span className="text-ink-faint">—</span> },
        { header: "Published", cell: (v) => <span className="text-ink-dim">{timeAgo(v.createdAt)}</span> },
      ]}
      empty={<p className="py-6 text-sm text-ink-dim">No versions.</p>}
    />
  );
}

function ApiDocs({ nav }: { nav: PackageNav | null }) {
  const modules = nav?.modules ?? [];
  if (modules.length === 0) return <p className="text-sm text-ink-dim">No API documentation.</p>;
  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_280px]">
      <div>
        <div className="text-lg font-semibold">Modules</div>
        <ul className="mt-2 space-y-1">
          {modules.map((m) => (
            <li key={m.name.go} className="flex items-center gap-2 text-sm">
              <span className="grid size-5 place-items-center rounded-full border border-line text-[10px] text-emerald-400">M</span>
              <span className="text-brand">{m.name.go}</span>
            </li>
          ))}
        </ul>
        {modules.map((m) => (
          <div key={m.name.go} className="mt-5">
            {m.functions?.length ? (
              <>
                <div className="text-base font-semibold">Functions</div>
                <ul className="mt-1 space-y-1 text-sm">
                  {m.functions.map((f) => <li key={f.typeToken} className="flex items-center gap-2"><span className="text-red-400">ƒ</span><span className="text-brand">{f.name.go}</span></li>)}
                </ul>
              </>
            ) : null}
            {m.resources?.length ? (
              <>
                <div className="mt-3 text-base font-semibold">Resources</div>
                <ul className="mt-1 space-y-1 text-sm">
                  {m.resources.map((r) => <li key={r.typeToken} className="flex items-center gap-2"><span className="text-violet-400">R</span><span className="text-brand">{r.name.go}</span></li>)}
                </ul>
              </>
            ) : null}
          </div>
        ))}
      </div>
      <aside className="border-l border-line pl-4">
        <div className="text-sm font-semibold">Index</div>
        {modules.map((m) => (
          <div key={m.name.go} className="mt-2 flex items-center gap-2 text-sm">
            <span className="rounded bg-emerald-500/15 px-1.5 text-xs text-emerald-400">mod</span>{m.name.go}
          </div>
        ))}
      </aside>
    </div>
  );
}
