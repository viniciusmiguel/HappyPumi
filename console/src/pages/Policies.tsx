import { useEffect, useState } from "react";
import { ShieldCheck, Plus } from "lucide-react";
import { api, type PolicyPack } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton } from "../components/ui";

export default function Policies() {
  const org = useOrg();
  const [packs, setPacks] = useState<PolicyPack[]>([]);
  useEffect(() => { api.policyPacks(org).then((r) => setPacks(r.policyPacks ?? [])); }, [org]);

  return (
    <div>
      <PageHeader icon={ShieldCheck} title="Policies" actions={<PrimaryButton icon={Plus}>New policy pack</PrimaryButton>} />
      <Table
        rows={packs}
        columns={[
          { header: "Policy pack", cell: (p) => <span className="font-medium">{p.displayName ?? p.name}</span> },
          { header: "Versions", cell: (p) => <span className="text-ink-dim">{p.versions?.length ?? "—"}</span> },
        ]}
        empty={<EmptyState icon={ShieldCheck} title="No policy packs"
          description="CrossGuard policy packs enforce guardrails across stacks before resources are provisioned."
          action={<PrimaryButton icon={Plus}>New policy pack</PrimaryButton>} />}
      />
    </div>
  );
}
