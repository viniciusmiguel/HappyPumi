import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Layers, Plus, Search } from "lucide-react";
import { api, timeAgo, type Stack } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, StatusDot } from "../components/ui";

export default function Stacks() {
  const org = useOrg();
  const [stacks, setStacks] = useState<Stack[]>([]);
  const [q, setQ] = useState("");

  useEffect(() => {
    // The seeded HappyPumi exposes the user's stacks directly; the project endpoint carries richer
    // per-stack detail (resourceCount/version/lastUpdate object), so merge both for the listing.
    api.userStacks().then(async (r) => {
      const base = r.stacks ?? [];
      const projects = [...new Set(base.map((s) => s.projectName))];
      const enriched = await Promise.all(projects.map((p) => api.project(org, p)));
      const byKey = new Map<string, Stack>();
      enriched.flatMap((e) => e.project.stacks).forEach((s) => byKey.set(`${s.projectName}/${s.stackName}`, s));
      setStacks(base.map((s) => byKey.get(`${s.projectName}/${s.stackName}`) ?? s));
    });
  }, [org]);

  const rows = stacks.filter((s) => `${s.projectName}/${s.stackName}`.includes(q));

  return (
    <div>
      <PageHeader icon={Layers} title="Stacks" actions={<PrimaryButton icon={Plus}>New stack</PrimaryButton>} />
      <div className="flex items-center gap-3 px-6 py-3">
        <div className="flex items-center gap-2 rounded-md border border-line bg-panel px-2.5 py-1.5 text-sm">
          <Search size={14} className="text-ink-faint" />
          <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search stacks"
            className="w-64 bg-transparent outline-none placeholder:text-ink-faint" />
        </div>
        <span className="text-sm text-ink-faint">{rows.length} stack{rows.length === 1 ? "" : "s"}</span>
      </div>
      <Table
        rows={rows}
        columns={[
          {
            header: "Stack",
            cell: (s) => (
              <Link to={`/stacks/${s.projectName}/${s.stackName}`} className="flex items-center gap-2 font-medium hover:underline">
                <Layers size={15} className="text-ink-faint" />
                {s.projectName}/{s.stackName}
              </Link>
            ),
          },
          { header: "Last update", cell: (s) => (
            <span className="flex items-center gap-2 text-ink-dim">
              <StatusDot status={s.lastUpdate?.result} />
              {timeAgo(s.lastUpdate?.endTime ?? s.lastUpdate?.time)}
            </span>
          ) },
          { header: "Resources", cell: (s) => <span className="text-ink-dim">{s.resourceCount ?? "—"}</span> },
          { header: "Version", cell: (s) => <span className="text-ink-dim">{s.lastUpdate?.version ?? s.version ?? "—"}</span> },
        ]}
        empty={<EmptyState icon={Layers} title="No stacks yet"
          description="A stack is an isolated, independently configurable instance of a Pulumi program. Run pulumi up to create one."
          action={<PrimaryButton icon={Plus}>New stack</PrimaryButton>} />}
      />
    </div>
  );
}
