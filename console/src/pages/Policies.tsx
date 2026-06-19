import { useEffect, useState } from "react";
import { ShieldCheck } from "lucide-react";
import { api, type PolicyPack } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState } from "../components/ui";

export default function Policies() {
  const org = useOrg();
  const [packs, setPacks] = useState<PolicyPack[]>([]);

  useEffect(() => { api.policyPacks(org).then((r) => setPacks(r.policyPacks ?? [])); }, [org]);

  return (
    <div>
      <PageHeader icon={ShieldCheck} title="Policies" />
      <p className="px-6 pt-3 text-sm text-ink-dim">
        Policy packs are published from the CLI with{" "}
        <code className="rounded bg-bg px-1">pulumi policy publish</code> and enabled per policy group; they
        appear here once published.
      </p>
      <div className="pt-3">
        <Table
          rows={packs}
          columns={[
            { header: "Policy pack", cell: (p) => <span className="font-medium">{p.displayName ?? p.name}</span> },
            { header: "Versions", cell: (p) => <span className="text-ink-dim">{p.versions?.length ?? "—"}</span> },
          ]}
          empty={<EmptyState icon={ShieldCheck} title="No policy packs"
            description="CrossGuard policy packs enforce guardrails across stacks before resources are provisioned. Publish one with pulumi policy publish." />}
        />
      </div>
    </div>
  );
}
