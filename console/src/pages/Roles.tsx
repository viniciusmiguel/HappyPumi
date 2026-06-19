import { useEffect, useState } from "react";
import { KeyRound, Plus } from "lucide-react";
import { api, type Role } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Field } from "../components/ui";

export default function Roles() {
  const org = useOrg();
  const [roles, setRoles] = useState<Role[]>([]);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "", description: "" });
  const [error, setError] = useState<string | null>(null);

  function load() { api.roles(org).then((r) => setRoles(r.roles ?? [])); }
  useEffect(() => { load(); }, [org]);

  async function createRole() {
    if (!form.name) return;
    setError(null);
    try {
      await api.createRole(org, form.name, form.description);
      setShowNew(false);
      setForm({ name: "", description: "" });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    }
  }

  return (
    <div>
      <PageHeader icon={KeyRound} title="Roles" actions={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>New role</PrimaryButton>} />
      <Table
        rows={roles}
        columns={[
          { header: "Name", cell: (r) => <span className="font-medium">{r.name}</span> },
          { header: "Description", cell: (r) => <span className="text-ink-dim">{r.description ?? "—"}</span> },
        ]}
        empty={<EmptyState icon={KeyRound} title="No custom roles"
          description="Create custom roles to grant fine-grained permissions across your organization."
          action={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>New role</PrimaryButton>} />}
      />

      {showNew && (
        <Modal title="Create a custom role" onClose={() => setShowNew(false)}
          footer={<>
            <SecondaryButton onClick={() => setShowNew(false)}>Cancel</SecondaryButton>
            <PrimaryButton onClick={createRole}>Create role</PrimaryButton>
          </>}>
          <Field label="Name" value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="Deployer" />
          <Field label="Description" value={form.description} onChange={(v) => setForm((f) => ({ ...f, description: v }))} placeholder="Can trigger deployments" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}
