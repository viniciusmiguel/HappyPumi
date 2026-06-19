import { useEffect, useState } from "react";
import { CheckSquare, Plus } from "lucide-react";
import { api, timeAgo, type ApprovalRule } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Field, Badge } from "../components/ui";

export default function Approvals() {
  const org = useOrg();
  const [rules, setRules] = useState<ApprovalRule[]>([]);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "", stackPattern: "*", requiredApprovals: "1" });
  const [error, setError] = useState<string | null>(null);

  function load() { api.approvalRules(org).then((r) => setRules(r.rules ?? [])); }
  useEffect(() => { load(); }, [org]);

  async function create() {
    if (!form.name) return;
    setError(null);
    try {
      await api.createApprovalRule(org, form.name, form.stackPattern || "*", Number(form.requiredApprovals) || 1);
      setShowNew(false); setForm({ name: "", stackPattern: "*", requiredApprovals: "1" }); load();
    } catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  }

  return (
    <div>
      <PageHeader icon={CheckSquare} title="Approvals"
        actions={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>New rule</PrimaryButton>} />
      <Table
        rows={rules}
        columns={[
          { header: "Rule", cell: (r) => <span className="font-medium">{r.name}</span> },
          { header: "Stack pattern", cell: (r) => <code className="text-ink-dim">{r.stackPattern}</code> },
          { header: "Required approvals", cell: (r) => <span className="text-ink-dim">{r.requiredApprovals}</span> },
          { header: "Status", cell: (r) => <Badge tone={r.enabled === false ? "default" : "success"}>{r.enabled === false ? "disabled" : "enabled"}</Badge> },
          { header: "Created", cell: (r) => <span className="text-ink-faint">{timeAgo(r.created)}</span> },
        ]}
        empty={<EmptyState icon={CheckSquare} title="No approval rules"
          description="Require sign-off before infrastructure changes are applied to matching stacks."
          action={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>New rule</PrimaryButton>} />}
      />
      {showNew && (
        <Modal title="Create an approval rule" onClose={() => setShowNew(false)}
          footer={<><SecondaryButton onClick={() => setShowNew(false)}>Cancel</SecondaryButton><PrimaryButton onClick={create}>Create</PrimaryButton></>}>
          <Field label="Name" value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="prod-requires-review" />
          <Field label="Stack pattern" value={form.stackPattern} onChange={(v) => setForm((f) => ({ ...f, stackPattern: v }))} placeholder="*/prod" />
          <Field label="Required approvals" value={form.requiredApprovals} onChange={(v) => setForm((f) => ({ ...f, requiredApprovals: v }))} placeholder="1" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}
