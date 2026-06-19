import { useEffect, useState } from "react";
import { GitBranch, Plus } from "lucide-react";
import { api, timeAgo, type VcsConnection } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Field, Badge } from "../components/ui";

const KINDS = ["github", "gitlab", "azuredevops", "bitbucket"];

export default function VersionControl() {
  const org = useOrg();
  const [connections, setConnections] = useState<VcsConnection[]>([]);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "", kind: "github" });
  const [error, setError] = useState<string | null>(null);

  function load() { api.vcsConnections(org).then((r) => setConnections(r.connections ?? [])); }
  useEffect(() => { load(); }, [org]);

  async function create() {
    if (!form.name) return;
    setError(null);
    try { await api.createVcsConnection(org, form.name, form.kind); setShowNew(false); setForm({ name: "", kind: "github" }); load(); }
    catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  }

  return (
    <div>
      <PageHeader icon={GitBranch} title="Version control"
        actions={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>Add account</PrimaryButton>} />
      <Table
        rows={connections}
        columns={[
          { header: "Account", cell: (c) => <span className="font-medium">{c.name}</span> },
          { header: "Provider", cell: (c) => <Badge tone="brand">{c.kind}</Badge> },
          { header: "Connected", cell: (c) => <span className="text-ink-faint">{timeAgo(c.created)}</span> },
        ]}
        empty={<EmptyState icon={GitBranch} title="Connect your version control system"
          description="Combine Pulumi with your VCS to enable pull request comments, policy enforcement, and drift detection."
          action={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>Add account</PrimaryButton>} />}
      />
      {showNew && (
        <Modal title="Connect version control" onClose={() => setShowNew(false)}
          footer={<><SecondaryButton onClick={() => setShowNew(false)}>Cancel</SecondaryButton><PrimaryButton onClick={create}>Connect</PrimaryButton></>}>
          <Field label="Account name" value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="acme-org" />
          <Field label="Provider" value={form.kind} onChange={(v) => setForm((f) => ({ ...f, kind: v }))} options={KINDS} />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}
