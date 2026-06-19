import { useEffect, useState } from "react";
import { Server, Plus } from "lucide-react";
import { api, timeAgo, type Service } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { PageHeader, Table, EmptyState, PrimaryButton, SecondaryButton, Modal, Field } from "../components/ui";

export default function Services() {
  const org = useOrg();
  const [services, setServices] = useState<Service[]>([]);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "", description: "" });
  const [error, setError] = useState<string | null>(null);

  function load() { api.services(org).then((r) => setServices(r.services ?? [])); }
  useEffect(() => { load(); }, [org]);

  async function create() {
    if (!form.name) return;
    setError(null);
    try { await api.createService(org, form.name, form.description); setShowNew(false); setForm({ name: "", description: "" }); load(); }
    catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  }

  return (
    <div>
      <PageHeader icon={Server} title="Services"
        actions={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>New service</PrimaryButton>} />
      <Table
        rows={services}
        columns={[
          { header: "Service", cell: (s) => <span className="font-medium">{s.name}</span> },
          { header: "Description", cell: (s) => <span className="text-ink-dim">{s.description || "—"}</span> },
          { header: "Created", cell: (s) => <span className="text-ink-faint">{timeAgo(s.created)}</span> },
        ]}
        empty={<EmptyState icon={Server} title="Create your first service"
          description="Services group stacks, environments and other resources in a way that makes sense to your organization."
          action={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>Create a new service</PrimaryButton>} />}
      />
      {showNew && (
        <Modal title="Create a service" onClose={() => setShowNew(false)}
          footer={<><SecondaryButton onClick={() => setShowNew(false)}>Cancel</SecondaryButton><PrimaryButton onClick={create}>Create</PrimaryButton></>}>
          <Field label="Name" value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="checkout" />
          <Field label="Description" value={form.description} onChange={(v) => setForm((f) => ({ ...f, description: v }))} placeholder="Checkout service stacks + environments" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}
