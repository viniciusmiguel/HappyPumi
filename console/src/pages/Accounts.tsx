import { useEffect, useState } from "react";
import { Cloud, Plus } from "lucide-react";
import { api, timeAgo, type CloudAccount } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Field, Badge } from "../components/ui";

const PROVIDERS = ["aws", "azure", "gcp", "kubernetes"];

export default function Accounts() {
  const org = useOrg();
  const [accounts, setAccounts] = useState<CloudAccount[]>([]);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "", provider: "aws", description: "" });
  const [error, setError] = useState<string | null>(null);

  function load() { api.cloudAccounts(org).then((r) => setAccounts(r.accounts ?? [])); }
  useEffect(() => { load(); }, [org]);

  async function create() {
    if (!form.name) return;
    setError(null);
    try { await api.createCloudAccount(org, form.name, form.provider, form.description); setShowNew(false); setForm({ name: "", provider: "aws", description: "" }); load(); }
    catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  }

  return (
    <div>
      <PageHeader icon={Cloud} title="Accounts"
        actions={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>Add account</PrimaryButton>} />
      <Table
        rows={accounts}
        columns={[
          { header: "Account", cell: (a) => <span className="font-medium">{a.name}</span> },
          { header: "Provider", cell: (a) => <Badge tone="brand">{a.provider}</Badge> },
          { header: "Description", cell: (a) => <span className="text-ink-dim">{a.description || "—"}</span> },
          { header: "Connected", cell: (a) => <span className="text-ink-faint">{timeAgo(a.created)}</span> },
        ]}
        empty={<EmptyState icon={Cloud} title="No cloud accounts"
          description="Connect cloud accounts to scan resources and detect drift with Pulumi Insights."
          action={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>Add account</PrimaryButton>} />}
      />
      {showNew && (
        <Modal title="Connect a cloud account" onClose={() => setShowNew(false)}
          footer={<><SecondaryButton onClick={() => setShowNew(false)}>Cancel</SecondaryButton><PrimaryButton onClick={create}>Connect</PrimaryButton></>}>
          <Field label="Name" value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="prod-aws" />
          <Field label="Provider" value={form.provider} onChange={(v) => setForm((f) => ({ ...f, provider: v }))} options={PROVIDERS} />
          <Field label="Description" value={form.description} onChange={(v) => setForm((f) => ({ ...f, description: v }))} placeholder="Production AWS account" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}
