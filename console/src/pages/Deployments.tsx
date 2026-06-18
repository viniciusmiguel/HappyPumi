import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Rocket } from "lucide-react";
import { api, timeAgo, type Deployment } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, Badge, StatusDot, Avatar } from "../components/ui";

const FILTERS = ["all", "running", "succeeded", "failed"];

export default function Deployments() {
  const org = useOrg();
  const [deps, setDeps] = useState<Deployment[]>([]);
  const [filter, setFilter] = useState("all");

  useEffect(() => { api.orgDeployments(org).then((r) => setDeps(r.deployments ?? [])); }, [org]);

  const rows = deps.filter((d) => filter === "all" || d.status.toLowerCase().includes(filter));

  return (
    <div>
      <PageHeader icon={Rocket} title="Deployments" />
      <div className="flex items-center gap-1.5 px-6 py-3">
        {FILTERS.map((f) => (
          <button key={f} onClick={() => setFilter(f)}
            className={`rounded-md px-2.5 py-1 text-sm capitalize transition-colors ${
              filter === f ? "bg-active text-ink" : "text-ink-dim hover:bg-hover"}`}>
            {f}
          </button>
        ))}
      </div>
      <Table
        rows={rows}
        columns={[
          { header: "Deployment", cell: (d) => (
            <Link to={`/deployments/${d.projectName}/${d.stackName}/${d.version}`} className="font-medium hover:underline">
              {d.projectName}/{d.stackName} #{d.version}
            </Link>
          ) },
          { header: "Operation", cell: (d) => <Badge>{d.pulumiOperation}</Badge> },
          { header: "Status", cell: (d) => <span className="flex items-center gap-2"><StatusDot status={d.status} />{d.status}</span> },
          { header: "Requested by", cell: (d) => (
            <span className="flex items-center gap-2 text-ink-dim"><Avatar name={d.requestedBy?.name} size={18} />{d.requestedBy?.name}</span>
          ) },
          { header: "Started", cell: (d) => <span className="text-ink-dim">{timeAgo(d.created)}</span> },
        ]}
        empty={<EmptyState icon={Rocket} title="No deployments" description="Deployments triggered across your stacks appear here." />}
      />
    </div>
  );
}
