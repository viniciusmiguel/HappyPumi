import { useEffect, useState } from "react";
import { ShieldCheck, Plus } from "lucide-react";
import { api, type PolicyPack } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Field } from "../components/ui";

export default function Policies() {
  const org = useOrg();
  const [packs, setPacks] = useState<PolicyPack[]>([]);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "" });

  useEffect(() => { api.policyPacks(org).then((r) => setPacks(r.policyPacks ?? [])); }, [org]);

  function createPack() {
    if (!form.name) return;
    setPacks((p) => [...p, { name: form.name, displayName: form.name, versions: [] }]);
    setShowNew(false);
    setForm({ name: "" });
  }

  return (
    <div>
      <PageHeader icon={ShieldCheck} title="Policies" actions={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>New policy pack</PrimaryButton>} />
      <Table
        rows={packs}
        columns={[
          { header: "Policy pack", cell: (p) => <span className="font-medium">{p.displayName ?? p.name}</span> },
          { header: "Versions", cell: (p) => <span className="text-ink-dim">{p.versions?.length ?? "—"}</span> },
        ]}
        empty={<EmptyState icon={ShieldCheck} title="No policy packs"
          description="CrossGuard policy packs enforce guardrails across stacks before resources are provisioned."
          action={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>New policy pack</PrimaryButton>} />}
      />

      {showNew && (
        <Modal title="Register a policy pack" onClose={() => setShowNew(false)}
          footer={<>
            <SecondaryButton onClick={() => setShowNew(false)}>Cancel</SecondaryButton>
            <PrimaryButton onClick={createPack}>Register</PrimaryButton>
          </>}>
          <Field label="Policy pack name" value={form.name} onChange={(v) => setForm(() => ({ name: v }))} placeholder="aws-guardrails" />
          <p className="text-xs text-ink-faint">Publish versions from the CLI with <code className="rounded bg-bg px-1">pulumi policy publish</code>.</p>
        </Modal>
      )}
    </div>
  );
}
