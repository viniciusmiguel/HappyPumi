import { useEffect, useState } from "react";
import { Layers, Plus } from "lucide-react";
import { api, type StackSummary } from "../lib/api";
import { PageHeader, Table, EmptyState, PrimaryButton } from "../components/ui";

export default function Stacks() {
  const [stacks, setStacks] = useState<StackSummary[]>([]);
  useEffect(() => { api.userStacks().then((r) => setStacks(r.stacks ?? [])); }, []);

  return (
    <div>
      <PageHeader icon={Layers} title="Stacks" actions={<PrimaryButton icon={Plus}>New stack</PrimaryButton>} />
      <Table
        rows={stacks}
        columns={[
          { header: "Stack", cell: (s) => <span className="font-medium">{s.orgName}/{s.projectName}/{s.stackName}</span> },
          { header: "Resources", cell: (s) => <span className="text-ink-dim">{s.resourceCount ?? "—"}</span> },
          { header: "Last update", cell: (s) => <span className="text-ink-dim">{s.lastUpdate ? new Date(s.lastUpdate * 1000).toLocaleString() : "—"}</span> },
        ]}
        empty={<EmptyState icon={Layers} title="No stacks yet"
          description="A stack is an isolated, independently configurable instance of a Pulumi program. Run pulumi up to create one."
          action={<PrimaryButton icon={Plus}>New stack</PrimaryButton>} />}
      />
    </div>
  );
}
